var response;

function logError(error) {
    console.error(error);
}

function handleError(error) {
    logError(error);
    response.send(500, error);
}

function expandVersion(version, asValue) {
    var digits = version.split('.');
    for (var idx = 0; idx < digits.length; idx++) {
        var d = digits[idx];
        d = ('0000' + d).substr(-4);
        digits[idx] = d;
    }
    if (asValue)
        return parseInt(digits.join(''), 10);
    else
        return digits.join('.');
}

exports.register = function (api) {
    api.get('/:version?', exports.get);
    api.post('/:version', exports.post);
};

exports.post = function(req, res) {
    /// <param name="req" type="ApiRequest"></param>
    /// <param name="res" type="ApiResponse"></param>
    response = res;
    var version = req.params.version;
    var versionHistory = req.service.tables.getTable('versionHistory');
    versionHistory.where({ version: version })
        .read({
            success: function (results) {
                if (results.length > 0) {
                    var item = {
                        id: results[0].id/*,
                        versionSort: expandVersion(version, true)*/
                    }
                    if (req.body.status)
                        item.status = req.body.status;
                    if (req.body.datePublished)
                        item.datePublished = req.body.datePublished;
                    if (req.body.changes)
                        item.historyData = JSON.stringify(req.body.changes);

                    versionHistory.update(item, {
                        success: function () { res.send(statusCodes.OK); },
                        error: handleError
                    })
                } else {
                    versionHistory.insert({
                        version: version,
                        versionSort: expandVersion(version, true),
                        status: req.body.status,
                        datePublished: req.body.datePublished,
                        historyData: JSON.stringify(req.body.changes)
                    }, {
                        success: function () { 
                            res.set('Location', req.host + req.path)
                            res.send(201); 
                        },
                        error: handleError
                    })
                }
            },
            error: handleError
        });
};

exports.get = function(req, res) {
    /// <param name="req" type="ApiRequest"></param>
    /// <param name="res" type="ApiResponse"></param>
    response = res;
    var version = req.params.version || req.query.version;
    var latest = version && version.toLowerCase() == 'latest';
    var published = version && version.toLowerCase() == 'published';
    var versionHistory = req.service.tables.getTable('versionHistory');
    var query = versionHistory;
    
    if (latest || published) {
        query = query.where({ status: "published" });
        if (latest)
            query = query.take(1);
    }
    else if (version)
        query = query.where({ version: version });
    query = query.orderByDescending('versionSort');
    
    query.read({
            success: function (results) {
                var result = [];
                if (results.length > 0) {
                    for (var idx = 0; idx < results.length; idx++) {
                        var element = results[idx];
                        result[idx] = {
                            version: element.version,
                            status: element.status,
                            datePublished: element.datePublished,
                            changes: JSON.parse(element.historyData)
                        }
                    }
                }
                res.send(statusCodes.OK, result);
            },
            error: handleError
        });
};