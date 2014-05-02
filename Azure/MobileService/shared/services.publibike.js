exports.serviceName = "publibike";
exports.cacheByCity = false;

exports.getUrl = function(cityName) {
    return "https://www.publibike.ch/en/stations.html";
}

exports.extractData = function (data, cityName) {
    var startMarker = "var aboProducts = [";
    var endMarker = "];";
    var start = data.indexOf(startMarker) + startMarker.length - 1;
    var end = data.indexOf(endMarker, start) + 1;
    data = data.substr(start, end - start);
    data = parseData(data, cityName);
    data = JSON.stringify(data);
    return data;
}

function parseData(data, cityName) {
    var aboList = JSON.parse(data);
    var stations = [];
    var idxStation = 0;
    var stationCache = [];
    aboList.forEach(function visitAbo(abo) {
        abo.abo.terminals.forEach(function visitTerminal(terminal) {
            if (!stationCache[terminal.terminalid]) {
                var holders = 0;
                var freeHolders = 0;
                var bikes = 0;
                terminal.bikeholders.forEach(function visitHolder(holder) {
                    holders += holder.holders;
                    freeHolders += holder.holdersfree;
                });
                terminal.bikes.forEach(function visitBike(bike) {
                    bikes += bike.available;
                });
                var station = {
                    id: terminal.terminalid,
                    name: terminal.name,
                    address: terminal.street,
                    city: terminal.city,
                    lat: terminal.lat,
                    lng: terminal.lng,
                    status: terminal.status,
                    bikes: bikes,
                    freeDocks: freeHolders,
                    totalDocks: holders
                }
                stations[idxStation++] = station;
                stationCache[terminal.terminalid] = true;
            }
        })
    });
    return stations;
}