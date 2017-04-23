###
###
### DEVICESIMULATOR
###
###

##
## Device-To-Cloud
##

#fake telemetry temperature North3
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "faketelemetry -hostNameiothubcourseeastus.azure-devices.net -deviceIdNorth3 -sharedAccessKeyaJKDY2eJoF/V47FTmtH02V4DS/XbbEeWtupDHDnigXE= -telemetrytypetemperature -tagsA,B,C -sleepmin200 -sleepmax1000 -initial-20 -deltamin-0.1 -deltamax0.1"

#telemetry temperature North3
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "telemetry -hostNameiothubcourseeastus.azure-devices.net -deviceIdNorth3 -sharedAccessKeyaJKDY2eJoF/V47FTmtH02V4DS/XbbEeWtupDHDnigXE= -telemetrytypetemperature -tagsA,B,C -current23"

#fake telemetry humidity South2
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "faketelemetry -hostNameiothubcourseeastus.azure-devices.net -deviceIdSouth2 -sharedAccessKeyQVyP5Plnz9FfqShdgoT5mX51brF3um9vgiWaot1yZyc= -telemetrytypehumidity -tagsC,D,F -sleepmin100 -sleepmax2000 -initial20 -deltamin-0.1 -deltamax0.1"

#sendevent North
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "sendevent -hostNameiothubcourseeastus.azure-devices.net -deviceIdNorth3 -sharedAccessKeyaJKDY2eJoF/V47FTmtH02V4DS/XbbEeWtupDHDnigXE= -eventTypetelemetry -eventBody{telemetrytype:'temperature',current:20}"









###
###
### DEVICESIMULATOR
###
###

##
## Device-To-Cloud Routing
##

#fake alert Alert1
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "fakealert -hostNameiothubcourseeastus.azure-devices.net -deviceIdAlert1 -sharedAccessKeyz/VZ2gkQ1+kbKyvPmA7viY4wGDug1dmRTcWlWUG6pFw= -alerttypesburning,empty,full,blocked -tagsA,D,F"

#alert Alert1
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "alert -hostNameiothubcourseeastus.azure-devices.net -deviceIdAlert1 -sharedAccessKeyz/VZ2gkQ1+kbKyvPmA7viY4wGDug1dmRTcWlWUG6pFw= -alerttypeburning -tagsA,D,F -severity78"

##
## Device-To-Cloud File upload
##

#fileupload North3
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList 'fileupload -hostNameiothubcourseeastus.azure-devices.net -deviceIdNorth3 -sharedAccessKeyaJKDY2eJoF/V47FTmtH02V4DS/XbbEeWtupDHDnigXE= -tagsA,B,C "-filenameD:\Cloud Academy\Introducing Azure IoTHub\source\Scripts\fileupload.png" -wait'
















##
## Cloud-To-Device
##

#handlemessage Alert1
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "handlemessage -hostNameiothubcourseeastus.azure-devices.net -deviceIdAlert1 -sharedAccessKeyz/VZ2gkQ1+kbKyvPmA7viY4wGDug1dmRTcWlWUG6pFw= -wait"
















#handleproperties Alert1
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "handleproperties -hostNameiothubcourseeastus.azure-devices.net -deviceIdAlert1 -sharedAccessKeyz/VZ2gkQ1+kbKyvPmA7viY4wGDug1dmRTcWlWUG6pFw="

#handledirectmethod Alert1
start-process -FilePath "..\bin\IoTHubDeviceSimulator.exe" -ArgumentList "handledirectmethod -hostNameiothubcourseeastus.azure-devices.net -deviceIdAlert1 -sharedAccessKeyz/VZ2gkQ1+kbKyvPmA7viY4wGDug1dmRTcWlWUG6pFw="
