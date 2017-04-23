
###
###
### DEVICEREGISTRY
###
###

##
## Device
##


##
## Twin
##

#gettwin Alert1
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "gettwin -deviceIdAlert1 -clipboard -wait"

#SetDesiredProperty Alert1 sleepmin 100
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "SetDesiredProperty -deviceIdAlert1 -propertynamesleepmin -propertyvalue100"

#SetDesiredProperty Alert1 sleepmin 500
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "SetDesiredProperty -deviceIdAlert1 -propertynamesleepmin -propertyvalue1000"

#SetDesiredProperty Alert1 sleepmax 500
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "SetDesiredProperty -deviceIdAlert1 -propertynamesleepmax -propertyvalue1000"

#SetDesiredProperty Alert1 sleepmax 1000
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "SetDesiredProperty -deviceIdAlert1 -propertynamesleepmax -propertyvalue3000"

#SetDesiredProperty Alert1 reportedcounter 0
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "SetDesiredProperty -deviceIdAlert1 -propertynamereportedcounter -propertyvalue100"

#Tag Alert1 location somewhere
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "tag -deviceIdAlert1 -tagnamelocation -tagvalueamerica"

#Tag North3 location somewhere
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "tag -deviceIdNorth3 -tagnamelocation -tagvalueamerica"

#Tag South2 location somewhere
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "tag -deviceIdSouth2 -tagnamelocation -tagvalueafrica"

#Tag Alert1 location somewhere
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "querytwin -querynameall -wait"

#Tag Alert1 location somewhere
start-process -FilePath "..\bin\IoTHubDeviceConfiguration.exe" -ArgumentList "querytwin -querynamebylocation -locationamerica -wait"
