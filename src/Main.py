from DataHandler import DataHandler
from models.EventMessage import EventMessage
from Common.Logger import Logger
from TelegramConnection import TelegramConnection
import Helpers

from datetime import datetime
import time, atexit, os, json, asyncio, configparser, asyncio
import paho.mqtt.client as mqtt

class FrigateSender:
    def __init__(self, Logger):
        
        self.Logger = Logger
        
        self.Config = configparser.ConfigParser()
        self.Config.read("config.ini")

        self.BaseURL = self.Config.get("Main", "BaseURL")
        self.SnapshotURL = self.Config.get("Main", "SnapShotURL")
        self.VideoURL = self.Config.get("Main", "VideoUrl")

        self.connectToMQTT()

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
        #if(rc == 16): # malformed package. Try again.
        
        # just try to connect again.
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
            event = new EventMessage
                    
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

        dateTimeText = datetime.today().strftime('%Y-%m-%d %H:%M:%S')
        
        # send picture right away on new events to get them out as fast as possible.
        try:
            if(eventType == "new"): #or eventType == "end"
                self.Logger.Info("Sending snapshot")
                # messageTextSnapshot = dateTimeText + ", id: " + str(eventId) + ", " + str(cameraName) + ", " + str(objectType) + ", score: " + str(score) + "."
                messageTextSnapshot = str(objectType).capitalize() + " in "  + str(cameraName) + ", s: " + str(score) + ", " + dateTimeText + ", id: " + str(eventId) + "."
                self.Logger.Info(messageTextSnapshot)
                await self.Sender.HandlePicture(snapShotUrl, messageTextSnapshot)

        except Exception as e:
            self.Logger.Error("Failed sending snapshot", str(e))
            self.Logger.Error("Crashed in HandleEvent.", e)
            self.Logger.Info(Helpers.format_stacktrace())

        # send out videos only after event ends so whole event is recorded.
        try:
            # only send video on end event
            if(eventType == "end"):
                self.Logger.Info("Sending video")
                messageTextVideo = "Id: " + str(eventId) + "."
                await self.Sender.HandleVideo(videoUrl, messageTextVideo) 

        except Exception as e:
            self.Logger.Error("Failed sending video", str(e))
            self.Logger.Error("Crashed in HandleEvent.", e)
            self.Logger.Info(Helpers.format_stacktrace())

        # cleanup
        for i in os.listdir(tempPath):
            self.Logger.Info("deleting: " + i)
            os.remove(os.path.join(tempPath, i))

    async def run(self):
        global RunApplication
        self.Sender = DataHandler(self.Logger)
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
            self.Logger.Error("Crashed in Main run.", e)
            self.Logger.Info(Helpers.format_stacktrace())

            MessagingTemp = TelegramConnection(Logger)
            await MessagingTemp.SendText("Bot crashed, restarting. Check my logs.")
            time.sleep(2)

def exit_handler():
    RunApplication = False

if __name__ == '__main__':

    Logger = Logger()   
    atexit.register(exit_handler)
    RunApplication = True
    
    while RunApplication:
        try:
            w = FrigateSender(Logger)
            asyncio.run(w.run())
            
        except Exception as e:
            Logger.Error("Crashed in Main start.", e)
            Logger.Info(Helpers.format_stacktrace())
            Logger.Info("Files may not have been sent. Sleeping 5 seconds before restart.")
            
            time.sleep(5)

    Logger.Info("Stopping")