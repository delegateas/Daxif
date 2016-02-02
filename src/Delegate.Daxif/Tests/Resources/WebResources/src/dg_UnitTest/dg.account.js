///<reference path="..\..\intellisense\dg.common.intellisense.js"/>
///<reference path="lib\dg.common.js" />
///<reference path="lib\dg.fetchXml.js" />
///<reference path="lib\dg.utils.js" />
///<reference path="lib\jquery.1.9.1.min.js"/>
///<reference path="lib\misc.json2.js"/>
///<reference path="lib\misc.uuid.js"/>
///<reference path="lib\sdk.metadata.js"/>
///<reference path="lib\sdk.rest.js"/>

if ("undefined" == typeof (DG)) {
    DG = { __namespace: true };
}

DG.Account = {
    onload: function () {
        // do
        // var fb = DG.Common.Field.getValue("foobar");

        // attach onChange
        DG.Common.Field.addOnChange("foo", DG.Account.foo);

        // attach onSave
        DG.Common.Form.addOnSave(DG.Account.bar);
    },
    foo: function () {
        // do
        // var f = DG.Common.Field.getValue("foo");
    },
    bar: function () {
        // do
        // DG.Common.Field.setValue("bar", 42);
    }
};
