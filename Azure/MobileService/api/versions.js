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

function Versions() {
    var self = this;
    self.response = null;
    self.azure = require('azure'); 
    var retryOperations = new self.azure.ExponentialRetryPolicyFilter();
    self.serviceData = self.azure.createTableService()
        .withFilter(retryOperations);

    self.logError = function(error) {
        console.error(error);
    };

    self.handleError = function(error) {
        self.logError(error);
        self.response.send(500, error);
    };

    self.post = function(req, res) {
    /// <param name="req" type="ApiRequest"></param>
    /// <param name="res" type="ApiResponse"></param>
        self.response = res;
        var version = req.params.version;
        if (!req.body) {
            res.send(400, 'request does not have a raw json data body');
            return;
        }
        
        var versionSort = expandVersion(version, true);
        versionSort = (1000000000000000 - versionSort).toString();

        self.serviceData.queryEntity('versionHistory', 'versions', versionSort, function(error, result) {
            if (!error || error.statusCode == 404) {
                var entity = result;
                if (!entity) {
                    entity = {
                        PartitionKey: 'versions', 
                        RowKey: versionSort,
                        version: version
                    }
                }

                if (req.body.status)
                    entity.status = req.body.status;
                if (req.body.datePublished)
                    entity.datePublished = req.body.datePublished;
                if (req.body.changes)
                    entity.historyData = JSON.stringify(req.body.changes);

                self.serviceData.insertOrReplaceEntity('versionHistory', entity,
                    function (error) {
                        if (error)
                            self.handleError(error);
                        else
                            res.send(200);
                    }
                );
            }
            else self.handleError(error);
        });
    };

    self.get = function(req, res) {
        /// <param name="req" type="ApiRequest"></param>
        /// <param name="res" type="ApiResponse"></param>
        self.response = res;
        var version = req.params.version || req.query.version;
        var latest = version && version.toLowerCase() == 'latest';
        var published = version && version.toLowerCase() == 'published';
        var query = self.azure.TableQuery
            .select()
            .from('versionHistory')
            .whereKeys('versions');

        if (latest || published) {
            query = query.where('status eq ?', 'published');
            if (latest)
                query = query.top(1);
        }
        else if (version) {
            var versionSort = expandVersion(version, true);
            versionSort = (1000000000000000 - versionSort).toString();
            query = query.where('RowKey eq ?', versionSort);
        }
        //query = query.orderByDescending('versionSort');
    
        self.serviceData.queryEntities(query, function(error, results) {
            if (!error) {
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
                res.send(200, result);
            }
            else self.handleError(error);
        });
    };
}


exports.register = function (api) {
    api.get('/:version?', exports.get);
    api.post('/:version', exports.post);
};

exports.post = function(req, res) {
    /// <param name="req" type="ApiRequest"></param>
    /// <param name="res" type="ApiResponse"></param>
    var versions = new Versions();
    versions.post(req, res);
};

exports.get = function(req, res) {
    /// <param name="req" type="ApiRequest"></param>
    /// <param name="res" type="ApiResponse"></param>
    var versions = new Versions();
    versions.get(req, res);
};
