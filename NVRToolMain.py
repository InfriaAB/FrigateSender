from subprocess import check_call
from pathlib import Path
import time, traceback, sys, atexit, os
import json
import asyncio
import threading
import configparser
from subprocess import check_output, Popen, PIPE
from datetime import datetime, timedelta
import logging
from logging.handlers import TimedRotatingFileHandler
from NVRHandleSend import NVRSender
from NVRLogger import Logger
from NVRTelegramConnection import TelegramConnection
import paho.mqtt.client as mqtt

## Requirements
## python3 -m pip install paho-mqtt --upgrade
## python3 -m pip install python-telegram-bot --upgrade

class SimpleNVR:
    def __init__(self, Logger):
        
        self.Logger = Logger
        
        self.Config = configparser.ConfigParser()
        self.Config.read("config.ini")

        self.BaseURL = self.Config.get("Main", "BaseURL")
        self.SnapshotURL = self.Config.get("Main", "SnapShotURL")
        self.VideoURL = self.Config.get("Main", "VideoUrl")

        self.connectToMQTT()

        tempSnapshotPath = os.path.realpath("test.mp4")
        fileNameOfSource = Path(tempSnapshotPath)
        splitFileNameStart = str(fileNameOfSource.with_suffix('')) + "_split_"
        splitFileNameComplete = splitFileNameStart + "%03d.mp4"

        Logger.Debug(fileNameOfSource)
        Logger.Debug(splitFileNameComplete)

        # splitCommand = f'{self.FFMPEGPath} -i "{tempSnapshotPath}" -c copy -map 0 -segment_time 00:00:{20} -f segment -reset_timestamps 1 -movflags +faststart "{splitFileNameComplete}"'
        # Logger.Debug(splitCommand)
        # os.system(splitCommand)

    def connectToMQTT(self):
        self.Logger.Info("MQTT: Connecting.")

        mqttAddress =  logLevel = self.Config.get("MQTT", "address")
        mqttPort =  logLevel = self.Config.getint("MQTT", "port")
        mqttTimeout =  logLevel = self.Config.getint("MQTT", "timeout")
        
        mqttUser =  logLevel = self.Config.get("MQTT", "user")
        mqttPass =  logLevel = self.Config.get("MQTT", "password")

        self.MQTTConnected = True

        self.mqttClient = mqtt.Client()
        self.mqttClient.username_pw_set(mqttUser, mqttPass)
        self.mqttClient.on_connect = self.on_connect
        self.mqttClient.on_message = self.on_message
        self.mqttClient.on_disconnect = self.on_disconnect

        self.mqttClient.connect(mqttAddress, mqttPort, mqttTimeout)

    def on_disconnect(self, client, userdata, rc):
        self.Logger.Info("MQTT: Disconnected, code = "+str(rc))

        self.MQTTConnected = False
        if(rc == 16): # malformed package. Try again.
            self.connectToMQTT()

    # The callback for when the client receives a CONNACK response from the server.
    def on_connect(self, client, userdata, flags, rc):
        #self.Logger.Info("Connected with result code "+ str(rc))

        if rc == 0:
            self.MQTTConnected = True
            client.subscribe("frigate/events")
            self.Logger.Info("MQTT: Subscribed OK")

        elif rc == 5:
            self.Logger.Info("MQTT: Unauthorized, code = " + str(rc))
            self.MQTTConnected = False
        else:
            self.Logger.Info("MQTT: Bad connection Returned code = "+ str(rc))
            self.MQTTConnected = False        

    # The callback for when a PUBLISH message is received from the server.
    def on_message(self, client, userdata, msg):

        # payload starts with b' -> https://stackoverflow.com/questions/40922542/python-why-is-my-paho-mqtt-message-different-than-when-i-sent-it
        if(str(msg.payload).startswith("b'")):
            msg.payload = msg.payload.decode("utf-8")      

        self.Logger.Info("New message on topic: " + str(msg.topic))

        try:
            eventId = ""
            eventType = ""
            cameraName = ""
            objectType = ""
            score = 0.0
            hasClip = False
            hasSnapshot = False

            data = json.loads(msg.payload)
            if "after" in data:
                self.Logger.Info("after is")
                if "id" in data["after"]:
                    eventId = data["after"]["id"]
                    self.Logger.Info(eventId)

                if "camera" in data["after"]:
                    cameraName = data["after"]["camera"]
                if "label" in data["after"]:
                    objectType = data["after"]["label"]
                if "score" in data["after"]:
                    score = round(float(data["after"]["score"]) * 100)
                if "has_clip" in data["after"]:
                    hasClip = data["after"]["has_clip"]                  
                if "has_snapshot" in data["after"]:
                    hasSnapshot = data["after"]["has_snapshot"]     
                if "type" in data:
                    eventType = data["type"]     
            
                self.Logger.Info(eventId + ", "+ eventType + ", " + cameraName + ", "+ objectType + ", "+ str(score) + ", "+ str(hasClip) + ", "+ str(hasSnapshot))
                self.Logger.Info(data)
                
                if(len(eventId) > 0):
                    run_async(self.HandleEvent(eventId, eventType, cameraName, objectType, score, hasClip, hasSnapshot))

                else:
                    self.Logger.Info("No event id from event")
                    
        except Exception as e: 
            self.Logger.Info(msg.payload)
            self.Logger.Error("Failed to parse MQTT message.", e)

    async def HandleEvent(self, eventId, eventType, cameraName, objectType, score, hasClip, hasSnapshot):
        tempPath = "temp"
        
        if(os.path.exists(tempPath) == False):
            os.makedirs(tempPath)

        self.Logger.Info(eventId + ", " + eventType + ", " + cameraName + ", "+ objectType + ", "+ str(score) + ", "+ str(hasClip) + ", "+ str(hasSnapshot))
        
        snapShotUrl = self.SnapshotURL.replace("{{base_url}}", self.BaseURL).replace("{{id}}", eventId)
        videoUrl = self.VideoURL.replace("{{base_url}}", self.BaseURL).replace("{{id}}", eventId).replace("{{camera}}", cameraName)
                
        self.Logger.Info(snapShotUrl)
        self.Logger.Info(videoUrl)

        # send snapshots on new events    ### and when event is over (best snapshot is chosen by Frigate to represent event).
        try:
            if(eventType == "new"): #or eventType == "end"
                self.Logger.Info("Sending snapshot")
                messageTextSnapshot = "Id: " + str(eventId) + ", " + str(cameraName) + ", " + str(objectType) + ", score: " + str(score) + "."
                await self.Sender.HandlePicture(snapShotUrl, messageTextSnapshot)

        except Exception as e:
            self.Logger.Error("Failed sending snapshot", str(e))
            self.Logger.Error("Crashed in HandleEvent.", e)
            self.Logger.Info(format_stacktrace())

        try:

            # only send video on end event
            if(eventType == "end"):
                self.Logger.Info("Sending video")
                messageTextVideo = "Id: " + str(eventId) + "."
                await self.Sender.HandleVideo(videoUrl, messageTextVideo) 

        except Exception as e:
            self.Logger.Error("Failed sending video", str(e))
            self.Logger.Error("Crashed in HandleEvent.", e)
            self.Logger.Info(format_stacktrace())

        # cleanup
        for i in os.listdir(tempPath):
            self.Logger.Info("deleting: " + i)
            os.remove(os.path.join(tempPath, i))

    async def run(self):
        global RunApplication
        self.Sender = NVRSender(self.Logger)
        await self.Sender.setup();
      
        try:
            counter = 0
            sleepTime = 0.5

            while RunApplication:
                self.mqttClient.loop(timeout=1.0)
                time.sleep(sleepTime)

                if self.MQTTConnected == False:
                    RunApplication = self.MQTTConnected

        except KeyboardInterrupt:
            self.Logger.Info("Exiting")
            RunApplication = False
            
        except Exception as e:
            self.Logger.Error("File watcher had an exception", str(e))
            self.Logger.Error("Crashed in NVRToolMain run.", e)
            self.Logger.Info(format_stacktrace())

            MessagingTemp = TelegramConnection(Logger)
            await MessagingTemp.SendText("Bot crashed, restarting. Check my logs.")
            time.sleep(2)

def format_stacktrace():
    parts = ["Traceback (most recent call last):\n"]
    parts.extend(traceback.format_stack(limit=25)[:-2])
    parts.extend(traceback.format_exception(*sys.exc_info())[1:])
    return "".join(parts)

_loop = asyncio.new_event_loop()
_thr = threading.Thread(target=_loop.run_forever, name="Async Runner", daemon=True)
def run_async(coro):  # coro is a couroutine, see example
    if not _thr.is_alive():
        _thr.start()
    future = asyncio.run_coroutine_threadsafe(coro, _loop)
    return future.result()

def exit_handler():
    RunApplication = False

atexit.register(exit_handler)

RunApplication = True




if __name__ == '__main__':

    Logger = Logger()   

    while RunApplication:
        try:
            w = SimpleNVR(Logger)
            asyncio.run(w.run())
            
        except Exception as e:
            Logger.Error("Crashed in NVRToolMain start.", e)
            Logger.Info(format_stacktrace())
            Logger.Info("Files may not have been sent. Sleeping 5 seconds before restart.")
            
            time.sleep(5)

    Logger.Info("Stopping")