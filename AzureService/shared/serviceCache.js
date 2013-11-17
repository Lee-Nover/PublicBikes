var downloadData = function () { };
var processData = function (data) { };

exports.serviceName = "";
exports.serviceData = null;

function logError(error) {
    console.error(error);
}

exports.updateCache = function (data, city) {
    var serviceData = exports.serviceData;
    var serviceName = exports.serviceName;
    if (serviceData === null)
        return;
    //console.info('updating cache for ' + serviceName);
    serviceData.where({ serviceName: serviceName, city: city })
        .read({
            success: function (results) {
                if (results.length > 0) {
                    serviceData.update({
                        id: results[0].id,
                        timeStamp: new Date(),
                        serviceData: data
                    }, {
                        //success: function () { console.info('updated cache for ' + serviceName); },
                        error: logError
                    })
                } else {
                    serviceData.insert({
                        serviceName: serviceName,
                        city: city,
                        timeStamp: new Date(),
                        serviceData: data
                    }, {
                        //success: function () { console.info('inserted cache data for ' + serviceName); },
                        error: logError
                    })
                }
            },
            error: logError
        })
}

exports.checkServiceData = function (city, onDownloadData, onProcessData) {
    var serviceData = exports.serviceData;
    var serviceName = exports.serviceName;
    if (serviceData === null) {
        onDownloadData();
        return;
    }
    //console.info('checking cached data for ' + serviceName);
    serviceData.where({ serviceName: serviceName, city: city })
        .read({
            success: function (results) {
                var isDataUsable = false;
                if (results.length > 0) {
                    var dataAge = Math.abs(new Date() - results[0].timeStamp);
                    isDataUsable = dataAge < 60000;
                    console.info('last available data for ' + serviceName,
                        {
                            dataAge: dataAge / 1000,
                            isUsable: isDataUsable
                        });
                }
                //else console.info('data not cached for ' + serviceName);
                if (isDataUsable)
                    onProcessData(results[0].serviceData)
                else
                    onDownloadData();
            },
            error: logError
        })
}