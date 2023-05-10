import os, sys
import configparser
import logging
from pathlib import Path
from logging.handlers import TimedRotatingFileHandler

class Logger:

    def __init__(self):
        self.Config = configparser.ConfigParser()
        self.Config.read("config.ini")

        logPath = self.Config.get("Logging", "Path")
        logLevel = self.Config.get("Logging", "LogLevel")

        tempPath = Path(logPath)
        
        if not os.path.exists(str(tempPath.parent)):
            print("Missing log folder, creating: " + str(tempPath.parent))
            os.makedirs(str(tempPath.parent))
        
        self.Logger = logging.getLogger("Rotating Log")
        self.Logger.setLevel(int(logLevel))

        self.Handler = TimedRotatingFileHandler(logPath,
                                                when='d',
                                                interval=1,
                                                backupCount=30,
                                                encoding=None,
                                                delay=False,
                                                utc=False)
        self.Logger.addHandler(self.Handler)

        self.Formatter = logging.Formatter(fmt='%(asctime)s %(levelname)-8s %(message)s',
                                  datefmt='%Y-%m-%d %H:%M:%S')

        self.Handler.setFormatter(self.Formatter)
        self.Screen_handler = logging.StreamHandler(stream=sys.stdout)
        self.Screen_handler.setFormatter(self.Formatter)
        self.Logger.addHandler(self.Screen_handler)

        self.Info("--- START ---")
        self.Info("Log level set to: " + logLevel)
        #self.Logger.handlers[0].doRollover()

    def Debug(self, text):
        self.Logger.debug(text)
    
    def Info(self, text):
        self.Logger.info(text)

    def Error(self, text, exception):
        #message = ("Log: " + text + os.linesep + "Exception: " + str(exception) + ".")
        self.Logger.warn(text)
        self.Logger.error(exception)
        
        
