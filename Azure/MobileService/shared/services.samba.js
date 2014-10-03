exports.serviceName = "samba";
exports.cacheByCity = true;

exports.getUrl = function(cityName) {
    return 'http://www.movesamba.com/' + cityName + '/mapaestacao.asp';
}

exports.extractData = function (data, cityName) {
    return LoadStationsFromHTML(data, cityName);
}

function LoadStationsFromHTML(s, cityName)
{
    var idxStation = 0;
    var stations = [];
    var CDataStr = 'exibirEstacaMapa(';
            
    var dataPos = s.indexOf(CDataStr);
    if (dataPos > 0)
        s = s.substr(dataPos, s.indexOf('function exibirEstacaMapa(', dataPos) - dataPos);
    else
        throw 'Unknown data format for samba : ' + cityName;
    dataPos = s.indexOf(CDataStr);
    while (dataPos > -1) {
        dataPos += CDataStr.length;
        var dataEndPos = s.indexOf(');', dataPos);
        var dataStr = s.substr(dataPos, dataEndPos - dataPos);
        var dataValues = eval('[' + dataStr + ']');
        // stations without the 'address' are test stations
        if (dataValues && dataValues.length > 9 && dataValues[9].trim() != '') {
            var available = parseInt(dataValues[7]);
            var free = parseInt(dataValues[8]);

            var station = {
                id: parseInt(dataValues[4]),
                name: dataValues[3],
                address: dataValues[9],
                city: cityName,
                lat: parseFloat(dataValues[0]),
                lng: parseFloat(dataValues[1]),
                status: dataValues[6] == 'EO' ? 1 : 0,
                bikes: available,
                freeDocks: free,
                totalDocks: available + free,
                connected: dataValues[7] == 'A'
            }
            stations[idxStation++] = station;
        }
        dataPos = s.indexOf(CDataStr, dataPos);
    }

    return JSON.stringify(stations);
}

function tryParseNum(s, def) {
    if (typeof s === 'number')
        return s;
    var rxNum = /\d+/;
    s = rxNum.exec(s);
    if (s == null || s === '')
        return def;
    return parseFloat(s);
}
