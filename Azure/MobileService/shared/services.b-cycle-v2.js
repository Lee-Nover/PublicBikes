exports.serviceName = 'B-cycle';
exports.cacheByCity = true;

exports.getHeaders = function (cityName) {
    return { ApiKey: process.env['b_cycle_apikey'] }
}

exports.getUrl = function (cityName, cityId) {
    var cityIds = {
        /* USA */
        arbor: 76,
        austin: 72,
        battlecreek: 71,
        santiago: 68,
        boulder: 54,
        broward: 53,
        charlotte: 61,
        cincinnatti: 80,
        columbiacounty: 74,
        dallas: 82,         /* n/a */
        denver: 36,
        desmoines: 45,
        dfc: 60,
        fargo: 81,          /* n/a */
        fortworth: 67,
        greenville: 65,
        hawaii: 49,
        houston: 59,
        indy: 75,
        kansascity: 62,
        madison: 55,
        milwaukee: 70,
        nashville: 64,
        omaha: 56,
        rapidcity: 79,
        saltlake: 66,
        sanantonio: 48,
        sanfranciscogride: 47,
        savannah: 73,
        spartanburg: 57,
        whippany: 77,        /* n/a */
        /* Chile */
        santiago: 68        
    }
    if (cityId == null)
        cityId = cityIds[cityName];
    return 'https://publicapi.bcycle.com/api/1.0/ListProgramKiosks/' + cityId;
}

exports.extractData = function (data, cityName, cityId) {
    /*
     * 
[
  {
    "Id": 1805,
    "PublicText": "",
    "Name": "B9",
    "Address": {
      "Street": "640 Forbes Blvd",
      "City": "San Fransico",
      "State": "CA",
      "ZipCode": "94080",
      "Country": "United States",
      "Html": "640 Forbes Blvd<br />San Fransico, CA 94080"
    },
    "Location": {
      "Latitude": 37.65760,
      "Longitude": -122.38164
    },
    "BikesAvailable": 9,
    "DocksAvailable": 6,
    "TotalDocks": 15,
    "HoursOfOperation": "06:30 - 19:00",
    "TimeZone": "(UTC-08:00) Pacific Time (US & Canada)",
    "Status": "Active",
    "IsEventBased": false
  }
]
     * 
     * 
     */


    var stationList = JSON.parse(data);
    var stations = [];
    var idxStation = 0;
    stationList.forEach(function visitStation(s) {
        var station = {
            id: s.Id,
            name: s.PublicText != '' ? s.PublicText : s.Name,
            address: s.Address.Street,
            city: cityName,
            lat: s.Location.Latitude,
            lng: s.Location.Longitude,
            status: s.Status == "Active" ? 1 : 0,
            bikes: parseInt(s.BikesAvailable),
            freeDocks: parseInt(s.DocksAvailable),
            totalDocks: parseInt(s.BikesAvailable) + parseInt(s.DocksAvailable)
        }
        stations[idxStation++] = station;
    });
    return JSON.stringify(stations);
}