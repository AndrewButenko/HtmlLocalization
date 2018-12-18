var AB = AB || {};

AB.HtmlLocalization = (function () {
    function initialize(webResources) {
        return new Promise(function(resolve, reject) {

            var ab_GetLocalizationRequest = {
                WebResources: JSON.stringify(webResources),
                getMetadata: function() {
                    return {
                        boundParameter: null,
                        parameterTypes: {
                            "WebResources": {
                                "typeName": "Edm.String",
                                "structuralProperty": 1
                            }
                        },
                        operationType: 0,
                        operationName: "ab_GetLocalization"
                    };
                }
            };

            Xrm.WebApi.online.execute(ab_GetLocalizationRequest).then(
                function success(result) {
                    if (result.ok) {
                        var results = JSON.parse(result.responseText);

                        var resultsObject = JSON.parse(results.Localizations);

                        for (var i in resultsObject) {
                            if (i === "DependencyNameToGuidMap") {
                                window[i] = JSON.parse(resultsObject[i]);
                            } else {
                                window[i] = resultsObject[i];
                            }
                        }
                    }

                    resolve();
                },
                function (error) {
                    reject(error);
                }
            );
        });
    }

    return {
        Initialize: initialize
    };
})();