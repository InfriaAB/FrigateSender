# FrigateSender
Send notifications and video from Frigate based on MQTT events to services such as Telegram without needing to open up Frigate to the internet.

This add-on also splits video files to smaller segments since most services limit filesizes and sends them one by one.

## The purpose
The main reason for creating this was that I did not like any of the other methods to send snapshots or videos to messaging services. All methods I found required you 
to open up home assistant to the internet, as well as them not handling videos exceeding the services file size limits (Telegram does not allow videos over 50Mb).