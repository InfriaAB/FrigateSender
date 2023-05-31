from Common.Logger import Logger
import time, json

class EventMessage:

    Data = None

    # 'eventId' contains the id of the event from Frigate.
    EventId = ""
    EventType = ""
    CameraName = ""
    ObjectType = ""
    Score = 0.0
    HasClip = False
    HasSnapshot = False

    IsParsed = False

    def __init__(self, eventMessage, Logger):
        
        if(eventMessage is None):
            Logger.Info("EventMessage.__init__: Event is null")
            return

        # payload starts with b' 
        # -> https://stackoverflow.com/questions/40922542/python-why-is-my-paho-mqtt-message-different-than-when-i-sent-it
        if(str(eventMessage.payload).startswith("b'")):
            eventMessage.payload = eventMessage.payload.decode("utf-8")      


        try:

            self.Data = json.loads(eventMessage.payload)
            if "after" in self.Data:
                Logger.Info("after is")
                if "id" in self.Data["after"]:
                    self.EventId = self.Data["after"]["id"]
                    Logger.Info(self.EventId)

                if "camera" in self.Data["after"]:
                    self.CameraName = self.Data["after"]["camera"]
                if "label" in self.Data["after"]:
                    self.ObjectType = self.Data["after"]["label"]
                if "score" in self.Data["after"]:
                    self.Score = round(float(self.Data["after"]["score"]) * 100)
                if "has_clip" in self.Data["after"]:
                    self.HasClip = self.Data["after"]["has_clip"]                  
                if "has_snapshot" in self.Data["after"]:
                    self.HasSnapshot = self.Data["after"]["has_snapshot"]     
                if "type" in self.Data:
                    self.EventType = self.Data["type"]     
            
                self.Logger.Info(self.EventId + ", "+ self.EventType + ", " + self.CameraName + ", "+ self.ObjectType + ", "+ str(self.Score) + ", "+ str(self.HasClip) + ", "+ str(self.HasSnapshot))
                self.Logger.Info(self.Data)
                
                if(len(eventId) < 0):
                    Logger.Info("No event id from event")

                self.isParsed = True
                    
        except Exception as e: 
            Logger.Info(msg.payload)
            Logger.Error("Failed to parse MQTT message.", e)