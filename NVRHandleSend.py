import os, sys, traceback, uuid, math, subprocess, configparser, time
import requests
import ssl
from pathlib import Path
from datetime import datetime, timedelta
from NVRTelegramConnection import TelegramConnection
from subprocess import check_call

class NVRSender:
    def __init__(self, Logger):
        self.Logger = Logger
        
        self.Config = configparser.ConfigParser()
        self.Config.read("config.ini")
        
        self.LastSendDate = datetime.now()
        self.LastSendCount = 0

        self.FFMPEGPath = self.Config.get("Main", "ffmpegPath")
        self.FFPROBEPath = self.Config.get("Main", "ffprobePath")

        self.RateLimit = int(self.Config.get("Main", "RateLimit"))
        self.RateLimitTimeout = int(self.Config.get("Main", "RateLimitTimeout"))
        self.FileMaxSize = int(self.Config.get("Main", "FileSizeMaxPerSend"))

        self.SSLCert = ssl.create_default_context()
        self.SSLCert.check_hostname = False
        self.SSLCert.verify_mode = ssl.CERT_NONE
    
    async def setup(self):
        self.Messaging = TelegramConnection(self.Logger)
        await self.Messaging.setup()

    # Rate limiting code to not send too many so we get blocked
    def SendThrottle(self):

        if(datetime.now() - self.LastSendDate) < timedelta(seconds=(int(self.RateLimitTimeout))):
            
            self.Logger.Debug("Item already sent within " + str(self.RateLimitTimeout) + " seconds.")

            if(self.LastSendCount < self.RateLimit):
                self.LastSendCount = self.LastSendCount + 1
                self.LastSendDate = datetime.now()
                self.Logger.Debug("Can still send more: " + str(self.LastSendCount) + " is less than " + str(self.RateLimit) + ".")
                return

            else:
                self.Logger.Debug("To many sent, sleeping for " + str(self.RateLimitTimeout) + " seconds.")
                time.sleep(int(self.RateLimitTimeout))
            
        #nothings been sent for a while, go for for it.
        self.Logger.Debug("Nothings been sent for a while, send it.")
        self.LastSendDate = datetime.now()
        self.LastSendCount = 1
        
        time.sleep(0.1) # General slow down so we dont spam telegram to much and have time for Frigate to finish creating files.

    async def HandlePicture(self, url, messageText):

        self.SendThrottle()

        tempSnapshotName = os.path.join("temp", "tempSnapshot.jpg")
        tempSnapshotPath = os.path.realpath(tempSnapshotName)

        with open(tempSnapshotPath, 'wb') as f:
            resp = requests.get(url, verify=False)
            f.write(resp.content)

        await self.Messaging.SendPhoto(tempSnapshotPath, messageText)
        
    async def HandleVideo(self, url, messageText):   
        
        # slow self down a bit since Frigate seems to not have video fully 
        # saved sometimes resulting in only partial video being downloaded.
        time.sleep(1)
        
        self.SendThrottle()

        tempVideoName = os.path.join("temp", "tempVideo.mp4")
        tempVideoPath = os.path.realpath(tempVideoName)

        attempts = 0
        size = 0
        while (attempts < 3 and size < 5):
            time.sleep(1)
            try:
                with open(tempVideoPath, 'wb') as f:
                    resp = requests.get(url, verify=False)
                    f.write(resp.content)
                
            except Exception as e:
                self.Logger.Error("NVRHandleSend.HandleVideo() crashed.", e)
                self.Logger.Info(self.format_stacktrace())

            size = self.GetFileSize(tempVideoPath)
            attempts = attempts + 1
            self.Logger.Debug("Video Download attempt: " + str(attempts) + ", size: " + str(size) + " Mb")
        
        self.Logger.Debug("Video download complete. File is " + str(size) + " Mb.")
        self.Logger.Debug(tempVideoPath)

        if(size >= self.FileMaxSize):
            self.Logger.Debug("File is to large and must be split (Max size: " + str(self.FileMaxSize) + " Mb).")
            
            videoFiles = self.SplitVideo(tempVideoPath, size);
            partCount = len(videoFiles)
            
            messageSplit = ("Sending video in " + str(partCount) + " parts.")
            self.Logger.Debug(messageSplit)
            await self.Messaging.SendText(messageSplit)

            counter = 0
            for videoPart in videoFiles:
                counter += 1
                self.Logger.Debug("Send part: " + str(videoPart))
                await self.Messaging.SendVideo(videoPart, str(messageText + " Part: " + str(counter) + "/" + str(partCount) + "."))     
        else:
            await self.Messaging.SendVideo(tempVideoPath, str(messageText)) 

    def SplitVideo(self, filePath, size):
        lengthSeconds = self.GetVideoLength(filePath)
        splitCount = math.ceil(size / self.FileMaxSize)
        secondsPerSplit = math.ceil(lengthSeconds / splitCount)

        self.Logger.Debug("Splitting one video of " + str(lengthSeconds) + " seconds.")

        fileNameOfSource = Path(filePath)
        splitFileNameStart = str(fileNameOfSource.with_suffix('')) + "_split_"
        splitFileNameComplete = splitFileNameStart + "%03d.mp4"
        splitCommand = f'{self.FFMPEGPath} -i "{filePath}" -c copy -map 0 -segment_time 00:00:{secondsPerSplit} -f segment -reset_timestamps 1 -movflags +faststart "{splitFileNameComplete}"'
       
        self.Logger.Debug(splitCommand)
        os.system(splitCommand)

        self.Logger.Debug("Result files name start: " + splitFileNameStart)
        filesToSend = self.GetAllFilesStartingWith(splitFileNameStart)
        self.Logger.Debug(str(filesToSend))
        
        return filesToSend

    def GetFileSize(self, filePath):
        size = os.path.getsize(filePath)/float(1<<20)
        return size
    
    def GetVideoLength(self, filePath):
        try:
            result = subprocess.run([self.FFPROBEPath, "-v", "error", "-show_entries",
                                 "format=duration", "-of",
                                 "default=noprint_wrappers=1:nokey=1", filePath],
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT)
            
            self.Logger.Debug("'" + filePath + "' is '" + str(result.stdout) + "' seconds long.")
            secondsLong = float(result.stdout)

            return secondsLong
            
        except Exception as e:
            self.Logger.Error("NVRHandleSend.GetVideoLength() crashed.", e)
            self.Logger.Info(self.format_stacktrace())

            # if we crashed on reading length of video, approximate by taking 0.75 Mb = 1 second
            # which should be a safe bet if camera is around 0.8 Mbit.
            # This might cause more than neccessary clip splits, but at least it should be well within margin.
            
            lengthFactor = 0.75
            
            fileSize = self.GetFileSize(filePath)
            guessedLength = max(int(math.ceil(fileSize) * lengthFactor), 1)
            self.Logger.Debug("Estimating clip length to: " + str(guessedLength) + " seconds based on " + str(fileSize) + " Mb size.")
            
            return guessedLength    

    def GetAllFilesStartingWith(self, filePathStart):
        filesDirectory = os.path.dirname(filePathStart)
        fileStart = Path(filePathStart).name

        resultFiles = []
        
        for file in os.listdir(filesDirectory):
            if file.startswith(fileStart):
                completePath = os.path.join(filesDirectory, file)
                resultFiles.append(completePath)

        resultFiles.sort()

        self.Logger.Debug("Found these files starting with: '" + filePathStart + "' -> " + str(resultFiles))
        return resultFiles
        
    def format_stacktrace(self):
        parts = ["Traceback (most recent call last):\n"]
        parts.extend(traceback.format_stack(limit=25)[:-2])
        parts.extend(traceback.format_exception(*sys.exc_info())[1:])
        return "".join(parts)