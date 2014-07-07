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
        case "aspen":
            return "https://www.we-cycle.org/pbsc/stations.php";
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
        case "aspen":
            return extractFromJson(data, cityName);
        // special json
        case "melbourne":
            return extractFromJson2(data, cityName);
        default:
            return data;
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
            freeDocks: parseInt($('nbEmptyDocks', co).text()),
            totalDocks: parseInt('0' + $('nbDocks', co).text())
        }
        if (!station.totalDocks)
            station.totalDocks = station.bikes + station.freeDocks;
        stations[idxStation++] = station;
    });

    return JSON.stringify(stations);
}

function extractFromJson(data, cityName) {
    var stationList = JSON.parse(data);
    var stations = [];
    var idxStation = 0;
    var useLandmark = cityName == 'chattanooga';
    stationList.stationBeanList.forEach(function visitStation(s) {
        var station = {
            id: s.id,
            name: useLandmark ? s.landMark : s.stationName,
            address: s.stAddress1,
            city: cityName,
            lat: s.latitude,
            lng: s.longitude,
            status: (!s.testStation && s.statusKey == 1) ? 1 : 0,
            bikes: parseInt(s.availableBikes),
            freeDocks: parseInt(s.availableDocks),
            totalDocks: parseInt(s.totalDocks)
        }
        if (!station.totalDocks)
            station.totalDocks = station.bikes + station.freeDocks;
        if (s.stAddress2 != '')
            if (s.stAddress1 != s.stationName)
                station.address += ', ' + s.stAddress2;
            else
                station.address = s.stAddress2;
        stations[idxStation++] = station;    
    });
    return JSON.stringify(stations);
}

function replaceAll(source, from, to) {
    return source.split(from).join(to);
}

function extractFromJson2(data, cityName) {
    // special characters encoded as hex values are not allowed in json, instead we convert them to unicode values
    data = replaceAll(data, '\\x', '\\u00');
    // apostrophes are unnecessarily escaped
    data = replaceAll(data, "\\'", "'");
    var stationList = JSON.parse(data);
    var stations = [];
    var idxStation = 0;
    stationList.forEach(function visitStation(s) {
        var station = {
            id: s.id,
            name: s.name,
            //address: '',
            city: cityName,
            lat: s.lat,
            lng: s.long,
            status: s.installed && !s.locked ? 1 : 0,
            bikes: parseInt(s.nbBikes),
            freeDocks: parseInt(s.nbEmptyDocks),
            totalDocks: parseInt(s.nbBikes) + parseInt(s.nbEmptyDocks)
        }
        if (!station.totalDocks)
            station.totalDocks = station.bikes + station.freeDocks;
        stations[idxStation++] = station;    
    });
    return JSON.stringify(stations);
}
