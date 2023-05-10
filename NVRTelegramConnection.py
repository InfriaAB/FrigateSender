import configparser
import asyncio
import telegram

class TelegramConnection:

    def __init__(self, Logger):
        self.Logger = Logger
        
        self.Config = configparser.ConfigParser()
        self.Config.read("config.ini")
        
        token = self.Config.get("Telegram", "Token")
        
        self.Bot = telegram.Bot(token)
        self.TargetChat = self.Config.get("Telegram", "ChatId")

    async def setup(self):
        me = await self.Bot.get_me()

        self.Logger.Info("TelegramBotName: " + me.username)
        self.Logger.Info("TargetChat: " + self.TargetChat)

        await self.SendText("NVRBot Online")

    async def SendText(self, text):
        self.Logger.Debug("Telegram -> Sending: '" + text + "'")
        await self.Bot.send_message(text=text, chat_id=self.TargetChat)
        self.Logger.Info("Sent text to Telegram")
    
    async def SendPhoto(self, filePath, messageText):
        self.Logger.Debug("Telegram -> Sending: " + filePath)
        await self.Bot.sendPhoto(self.TargetChat, photo=open(filePath, 'rb'), caption=str(messageText))
        self.Logger.Info("Sent photo to Telegram")

    async def SendVideo(self, filePath, messageText):
        try:
            self.Logger.Debug("Telegram -> Sending: " + filePath)
            await self.Bot.sendVideo(self.TargetChat, video=open(filePath, 'rb'), supports_streaming=True, caption=str(messageText), write_timeout=300)
            self.Logger.Info("Sent video to Telegram")
        except Exception as e:
            self.Logger.Error("Failed sending video", e)
            self.Logger.Info(self.format_stacktrace())
        
        # read_timeout: ODVInput[float] = DEFAULT_NONE,
        # write_timeout: ODVInput[float] = 20,
        # connect_timeout: ODVInput[float] = DEFAULT_NONE,
        # pool_timeout: ODVInput[float] = DEFAULT_NONE,        

    def format_stacktrace(self):
        parts = ["Traceback (most recent call last):\n"]
        parts.extend(traceback.format_stack(limit=25)[:-2])
        parts.extend(traceback.format_exception(*sys.exc_info())[1:])
        return "".join(parts)