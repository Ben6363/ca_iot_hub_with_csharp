###
###
### DEVICESERVICE
###
###

##
## Device-To-Cloud
##

#handlefileupoad
start-process -FilePath "..\bin\IoTHubDeviceService.exe" -ArgumentList "handlefileupload"
















##
## Cloud-To-Device
##

#sendmessage Alert1
start-process -FilePath "..\bin\IoTHubDeviceService.exe" -ArgumentList "sendmessage -deviceIdAlert1 -messagebody{'a':1,'b':2}"

#sendmessage Alert1 ack
start-process -FilePath "..\bin\IoTHubDeviceService.exe" -ArgumentList "sendmessage -deviceIdAlert1 -messagebody{'a':1,'b':2} -ack"





















#directmethod uptime Alert1
start-process -FilePath "..\bin\IoTHubDeviceService.exe" -ArgumentList "directmethod -deviceIdAlert1 -methodnameuptime -wait"

#directmethod uptime North3
start-process -FilePath "..\bin\IoTHubDeviceService.exe" -ArgumentList "directmethod -deviceIdNorth3 -methodnameuptime -wait"

#purgemessagequeue uptime North3
start-process -FilePath "..\bin\IoTHubDeviceService.exe" -ArgumentList "purgemessagequeue -deviceIdNorth3"
