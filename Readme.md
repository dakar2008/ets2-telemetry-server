## ETS2 Telemetry Web Server 4.0.0

This is a free Telemetry Web Server for [Euro Truck Simulator 2](http://www.eurotrucksimulator2.com/) and [American Truck Simulator](http://www.americantrucksimulator.com/) written in C# based on WebSockets and REST API.

## NOTE

This is an updated version of Funbit's Telemetry server so it can handle the newest telemetry data from SCS Software's SDK latest stable version - 1.10

This has been updated by dakar2008 and with a lot of help from Jonas Fabisiak aka (Rencloud) who has his own ETS2 telemetry plugin here: https://github.com/RenCloud/scs-sdk-plugin

This version of Funbit's telemetry server does only support REST API, no HTML5 Dashboard or Mobile version, it may come in a later version, but with this REST API you can build your own Applications.

## NOTE

## Main Features

- Free and open source
- Automated installation
- REST API for telemetry data
- Telemetry data broadcasting to a given URL via HTTP protocol

### Telemetry REST API
  
    GET http://localhost:25555/api/ets2/telemetry

Returns structured JSON object with the latest telemetry data read from the game: 

    {    
		"game": {
			"connected": true,
			"paused": false,
			"gameName": "ETS2",
			"time": "0001-01-08T21:09:00Z",
			"timeScale": 19.0,
			"nextRestStopTime": "0001-01-01T10:11:00Z",
			"version": "1.10",
			"telemetryPluginVersion": "7"
		},
		"truck":{
			"id": "man",
			"make": "MAN",
			"model": "TGX",
			"speed": 53.82604,
			... 
    }

The state is updated upon every API call. You may use this REST API for your own Applications.