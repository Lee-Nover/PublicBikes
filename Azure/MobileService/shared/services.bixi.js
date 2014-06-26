exports.serviceName = "bixi";
exports.cacheByCity = true;

exports.getUrl = function(cityName) {
    switch (cityName) {
        // xml data
        case "london":
            return "http://www.tfl.gov.uk/tfl/syndication/feeds/cycle-hire/livecyclehireupdates.xml";
        case "washingtondc":
            return "http://www.capitalbikeshare.com/data/stations/bikeStations.xml";
        case "minneapolis":
            return "https://secure.niceridemn.org/data2/bikeStations.xml";
        case "boston":
            return "http://www.thehubway.com/data/stations/bikeStations.xml";
        case "toronto":
            return "https://toronto.bixi.com/data/bikeStations.xml";
        case "ottawa":
            return "https://capital.bixi.com/data/bikeStations.xml";
        case "montreal":
            return "https://montreal.bixi.com/data/bikeStations.xml";
        // json data
        case "melbourne":
            return "http://www.melbournebikeshare.com.au/stationmap/data"; // special json
        case "chattanooga":
            return "http://www.bikechattanooga.com/stations/json";
        case "newyork":
            return "https://citibikenyc.com/stations/json";
        case "chicago":
            return "https://divvybikes.com/stations/json";
        case "sanfrancisco":
            return "http://bayareabikeshare.com/stations/json";
        // unknown
        default:
            return "http://bixi.com";
    }
}

exports.extractData = function (data, cityName) {
    switch (cityName) {
        // xml data
        case "london":
        case "washingtondc":
        case "minneapolis":
        case "boston":
        case "toronto":
        case "ottawa":
        case "montreal":
            return extractFromXML(data, cityName);
        // json data
        case "chattanooga":
        case "newyork":
        case "chicago":
        case "sanfrancisco":
            return extractFromJson(data, cityName);
        // special json
        case "melbourne":
            return extractFromJson2(data, cityName);
        default:
            break;
    }
}

function extractFromXML(data, cityName) {
    /*
        <station>
          <id>2</id> 
          <name>100 Main Street SE</name> 
          <terminalName>30000</terminalName> 
          <lastCommWithServer>1403550999483</lastCommWithServer> 
          <lat>44.984892</lat> 
          <long>-93.256551</long> 
          <installed>true</installed> 
          <locked>false</locked> 
          <installDate>0</installDate> 
          <removalDate>0</removalDate> 
          <temporary>false</temporary> 
          <public>true</public> 
          <nbBikes>11</nbBikes> 
          <nbEmptyDocks>14</nbEmptyDocks> 
          <latestUpdateTime>0</latestUpdateTime> 
        </station>
    */

    var stations = [];
    var idxStation = 0;
    var cheerio = require('cheerio');
    var filterName = /^(\[?\]?\s?\d+\]?\[?(\s?)+)/;
    $ = cheerio.load(data, { ignoreWhitespace: true, xmlMode: true} ); // load the html nodes
    var xmlstations = $('station');
    xmlstations.each(function(i, item) {
        var co = $(item);
        var installed = $('installed', co).text();
        var locked = $('locked', co).text();
        var station = {
            id: parseInt($('id', co).text()),
            name: $('name', co).text(),
            //address: address,
            city: cityName,
            lat: parseFloat($('lat', co).text()),
            lng: parseFloat($('long', co).text()),
            status: ((installed == 'true' && locked == 'false')) ? 1 : 0,
            bikes: parseInt($('nbBikes', co).text()),
            emptyDocks: parseInt($('nbEmptyDocks', co).text()),
            totalDocks: parseInt($('nbDocks', co).text())
        }
        station.totalDocks = station.bikes + station.emptyDocks;
        stations[idxStation++] = station;
    });

    return JSON.stringify(stations);
}

function extractFromJson(data, cityName) {
    var startMarker = 'markerOptions":[';
    var endMarker = '}],';
    var start = data.indexOf(startMarker) + startMarker.length - 1;
    var end = data.indexOf(endMarker, start) + 2;
    data = data.substr(start, end - start);
    data = JSON.parse(data);

    var stations = [];

    var index = 1;
    var idxStation = 0;
    var cheerio = require('cheerio');
    var rxGetNum = /\d+/;
    
    data.forEach(function visitMarker(marker) {
        var info = marker.info;
        $ = cheerio.load(info); // load the html nodes
        var avail = $('li');
        var available = parseInt(rxGetNum.exec($($(avail)[0]).text()));
        var free = parseInt(rxGetNum.exec($($(avail)[1]).text()));
        var title = marker.title;
        var idxTitle = title.indexOf('-');
        if (idxTitle > 0) {
            index = parseInt(title.substr(0, idxTitle-1).trim());
            title = title.substr(idxTitle+1).trim();
        }
        var station = {
            id: index++,
            name: title,
            address: '',
            city: cityName,
            lat: marker.position.lat,
            lng: marker.position.lng,
            status: 1,
            bikes: available,
            freeDocks: free,
            totalDocks: available + free
        }
        stations[idxStation++] = station;
    });
    return JSON.stringify(stations);
}

function extractFromJson2(data, cityName) {
    var stationList = JSON.parse(data);
    var stations = [];
    var idxStation = 0;
    var filterName = /^([\d\s-])*/;
    stationList.forEach(function visitStation(s) {
        var station = {
            id: s.id,
            name: s.name.replace(filterName, ''),
            address: s.address.replace(filterName, ''),
            city: cityName,
            lat: s.lat,
            lng: s.lon,
            status: s.status == "OPN" ? 1 : 0,
            bikes: parseInt(s.bikes),
            freeDocks: parseInt(s.slots),
            totalDocks: parseInt(s.bikes) + parseInt(s.slots)
        }
        stations[idxStation++] = station;    
    });
    return JSON.stringify(stations);
}
