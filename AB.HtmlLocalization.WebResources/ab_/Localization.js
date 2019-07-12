var Localization = (function () {

    function localizeLabel() {
AB.HtmlLocalization.Initialize(["ab_/Localization.js"])
    .then(function() {
            var a = Xrm.Utility.getResourceString("ab_/Messages", "LocalizedMessage");
            console.log("Localized Value - " + a);
        },
        function(e) {
            console.log(e);
        });
    }

    return {
        LocalizeLabel: localizeLabel
    };

})();