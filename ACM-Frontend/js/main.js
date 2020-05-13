/**
 * Production Flag
 */
var isProduction = false;

/**
 * custom url that can be set using sessionStorage for testing. This should not be used in production
 */
var customBaseURL = sessionStorage.getItem("customBaseURL");


var BASE_URL = "https://solutions-dev.getcloudcherry.com:8801";


/**
 * Global configuration for setting the endpiont
 */
var config = {
    baseURL: isProduction || !customBaseURL ? BASE_URL : customBaseURL,
};

/**
 * SELECTOR INPUT MAPPING
 */

/**
 * Message Bird specific form map
 */
var messageBirdVendor = {
    MessageUrl: "#getMessageUrl",
    accessKey: "#getAccessKey",
    originator: "#getOriginator",
};

/**
 * Spark Post specific form map
 */
var sparkPostVendor = {
    url: "#getSparkUrl",
    api: "#getSparkApiKey",
    email: "#getSparkSenderEmail",
    getSparkSenderName: "#getSparkSenderName",
    getSparkBatchSize: "#getSparkBatchSize",
};

/**
 * Custom SMS form map
 */
var customSms = {
    getSmsUrl: "#getSmsurl",
};

/**
 * Custom SMTP form map
 */
var customSmtp = {
    senderName: "#getSenderName",
    email: "#getEmailAddress",
    smtpServer: "#getSmtpServer",
    smtpUsername: "#getSmtpUsername",
    smtpPassword: "#getSmtpPassword",
    port: "#getPort",
};

var notificationForm = {
    "d-notification":"#d-notification",
    "i-notification":"#i-notification",
    "w-notification":"#w-notification",
    "e-notification":"#e-notification",
    "f-notification":"#f-notification"
}

var superAdminform ={
    "admin-notification-multi-email":"#admin-notification-multi-email"
}

/**
 * Required validation will run of these elements
 */
var fieldsWithRequiredValidators = [
    "#getSenderName",
    "#getEmailAddress",
    "#getSmtpServer",
    "#getSmtpUsername",
    "#getSmtpPassword",
    "#getPort",
    "#getSmsurl",
    "#getMessageUrl",
    "#getAccessKey",
    "#getOriginator",
    "#getSparkUrl",
    "#getSparkApiKey",
    "#getSparkSenderName",
    "#getSparkBatchSize",
];
/**
 * Email validator will run on these elements
 */
var fieldsWithEmailValidators = ["#getEmailAddress", "#getSparkSenderEmail"];

/**
 * Multi Email notification validation no validator will run on these elements
 */

var fieldsWithMultiEmailValidators = ["#d-notification", "#i-notification","#w-notification","#e-notification","#f-notification"];

var fieldSuperAdminNotificationValidator = ["#admin-notification-multi-email"]

var prefillArrayValue;
//Global variable declaration
var staffPrefillArray = [];
var auth_token;
var emailpostresponse;
buttonClickCount = 0;
var dispatchList;
var getVendorByName;
var getUpdateDispatcherValue;

/**
 * Utility and validators
 */
// It will return the paritcular element for the validator object
function getElement(selector) {
    return document.querySelector(selector);
}

// function for email validation for all vendor details
function emailFormat(element) {
    var str = element.value;
    var patt = /^(([^<>()\[\]\.,;:\s@\"]+(\.[^<>()\[\]\.,;:\s@\"]+)*)|(\".+\"))@(([^<>()[\]\.,;:\s@\"]+\.)+[^<>()[\]\.,;:\s@\"]{2,})$/i;
    var res = patt.test(String(str).toLowerCase());
    if (element.value === "") {
        $(element).next().remove();
        var value = $(element).closest('.form__group').find('.form__label').text();
        $(element).after(`<span class="form-error-msg">${value} is required</span>`
        );
        return true;
    } else if (res === false) {
        $(element).next().remove();
        $(element).after(
            `<span class="form-error-msg">Incorrect email format. Please check and try again.</span>`
        );
        return true;
    } else {
        $(element).next().remove();
        return false;
    }
}

// function for required string validation for all vendor details
function required(element) {
    if (element.value === "") {
        $(element).next().remove();
        var value = $(element).closest('.form__group').find('.form__label').text();
        $(element).after(`<span class="form-error-msg">${value} is required</span>`
        );
        return true;
    } else {
        $(element).next().remove();
        return false;
    }
}

//Notification email validation
function notificationEmailValidation(element){
    var value = element.value;
if (value !== "" && validate(value) === false){
  $(element).closest('.form__group').find('.notification-error').remove();
  $(element).after(`<span class="form-error-msg notification-error">Some email(s) are in incorrect
  email format. Please check and try again.</span>`);
  $("#generate-dispatcher").attr("disabled", true);
  document.querySelector(".error-save-vendor").style.display = "block"
}else{
  $(element).closest('.form__group').find('.notification-error').remove();
  document.querySelector(".error-save-vendor").style.display = "none"
  $("#generate-dispatcher").attr("disabled", false);
}
}

function superAdminNotificationEmailValidation(element){
    var value = element.value;
if (value !== "" && validate(value) === false){
  $(element).closest('.form__group').find('.notification-error').remove();
  $(element).after(`<span class="form-error-msg notification-error">Some email(s) are in incorrect
  email format. Please check and try again.</span>`);
}else{
  $(element).closest('.form__group').find('.notification-error').remove();
}
}
/**
 * Sign in to get OAuth token
 */
function getDetails() {
    $(".button-submit").append(
        '<i class="fas fa-circle-notch fa-spin fa-lg"></i>'
    );
    $(".button-submit span").hide();
    $(".button-submit").attr("disabled", true);
    user = {
        Username: document.getElementById("username").value,
        Password: document.getElementById("password").value,
    };
    getAuthenticationToken(user);
}

function hidespinner(){
    $(".button-submit .fa-spin").hide();
    $(".button-submit span").show();
    $(".button-submit").attr("disabled", false);
}
// post the login details and  generate the login token to login into config page
function getAuthenticationToken(user) {
    var settings = {
        async: true,
        crossDomain: true,
        url: config.baseURL + "/api/config/login",
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        data: JSON.stringify(user),
        
        error: function (xhr, error) {
            // disable the loading icon and enable the text in button as well as showing error message
            
           var resMsg = JSON.parse(xhr.responseText);
           if(resMsg.isSuccessful === false){
           document.getElementById("show-error").innerHTML = resMsg.message;
           hidespinner();
           
            }
            else if(user.Password === "" && user.Username === "" ){
                document.getElementById("show-error").innerHTML = 'The Username/Password field is required';
                hidespinner();
            }
            else if(user.Username === "")
            {
                document.getElementById("show-error").innerHTML = 'The Username field is required';
                hidespinner();
            }
            else if(user.Password === ""){
                document.getElementById("show-error").innerHTML = 'The Password field is required';
                hidespinner();
            }
            
        },
    };
    $.ajax(settings).done(function (oResponse) {
   
        if (oResponse) {
            //get localStorge token and go to login page
            auth_token = oResponse.message;
            sessionStorage.setItem("Oauth_Token", auth_token);
            var current = window.location.href;
            var i = current.lastIndexOf("/");
            if (i != -1) {
                current = current.substr(0, i) + "/config-file.html";
            }
            window.open(current, "_self");
        }
    });
}

// Create dispatches list in the dropdown select and queue name / queue connection string
function getDispatcherlist() {
    // go to dispatcher list
    auth_token = sessionStorage.getItem("Oauth_Token");
    var settings = {
        async: true,
        crossDomain: true,
        url: config.baseURL + "/api/config/dispatch",
        method: "GET",
        headers: {
            Authorization: "Bearer " + auth_token,
        },
        statusCode: {
            401: function () {
                //when login token is expired. alert message is popuped and go to login page
                alert(
                    "Login token is expired. Please logout and login again to get latest changes"
                );
                var current = window.location.href;
                var i = current.lastIndexOf("/");
                if (i != -1) {
                    current = current.substr(0, i) + "/index.html";
                }

                window.open(current, "_self");
            },
        },
        error: function (xhr, error) {
            // show the error message when API is fail
            document.querySelector(".select-dispatcher-list").style.display = "block";
            document.getElementById("error-dispatch-name").style.display = "block";
            document.getElementById("icon-block").style.display = "none";
        },
    };
    $.ajax(settings).done(function (data) {
        if (data) {
            // get the queuetype and queue connection string and display it in the bottom of the page
            if (data.queue.queueType === "") {
                document.getElementById("queue-vendor-name-error").style.display =
                    "block";
            } else {
                document.getElementById("Queue-Vendor-name").innerHTML =
                    data.queue.queueType;
            }
            if (data.queue.queueConnectionString === "") {
                document.getElementById("storage-account-error").style.display =
                    "block";
            } else {
                document.getElementById("Storage-Account").innerHTML =
                    data.queue.queueConnectionString;
            }
            dispatchList = data;
            for (var i = 0; i < dispatchList.dispatches.length; i++) {
                // create the dispatches list in the dropdown
                $("#getdispatchers")
                    .append(`<option questionId="${dispatchList.dispatches[i].Key}" value="${dispatchList.dispatches[i].Value}"> 
             ${dispatchList.dispatches[i].Value} 
            
        </option>`);
                document.getElementById("icon-block").style.display = "none";
                document.querySelector(".select-dispatcher-list").style.display = "block";
            }
        }
    });
}

// this is used to call the dispatcher API by questionID is select in the select-field
function getDisptachById(data) {
    document.getElementById("configuration-block").style.display = "none";
    document.getElementById("icon-block").style.display = "block";
    document.getElementById("getdispatchers").options[0].disabled = true;
    const url = config.baseURL + "/api/config/dispatch/" + data;
    var settings = {
        async: true,
        crossDomain: true,
        url: url,
        method: "GET",
        headers: {
            Authorization: "Bearer " + auth_token,
        },
        statusCode: {
            401: function () {
                // token is expired it will go back to the login page and show alert message
                alert("Login token is expired. Please logout and login again");
                var current = window.location.href;
                var i = current.lastIndexOf("/");
                if (i != -1) {
                    current = current.substr(0, i) + "/index.html";
                }

                window.open(current, "_self");
            },
        },

        error: function (xhr, error) {
            // display the error message when the vendor details is not availble
            document.getElementById("icon-block").style.display = "none";
            document.getElementById("configuration-block").style.display = "none";
            document.getElementById("error-dispatch-name").innerHTML =
                "No vendor available for Email / SMS. Please check the database and try again.";
        },
    };
    $.ajax(settings).done(function (data) {

        getUpdateDispatcherValue = data;
     
        
        $("#static-select-prefills").find("option[questionId]").remove(); // remove the all options in the static response -> select question prefill
        $("#buildyourform .fieldwrapper .form__group:last-child").hide(); // hide static reponses text field
        $(".error-noti").hide();
        $("#buildyourform1 div").remove(); // remove added form field in the static response
        $("#static-select-prefills").remove(); // remove static response div
        document.getElementById("static-response-error").style.display = "none";
        // check static prefill is empty or not
        if (
            typeof getUpdateDispatcherValue.staticPrefills !== "undefined" &&
            getUpdateDispatcherValue.staticPrefills.length > 0
        ) {
            getElement(".static-response").style.display = "block";
            $("#select-response-static").append(
                ' <select name="notes" value="" onchange="selectPrefill()" id="static-select-prefills" class="fieldtype select-text"> <option selected value="Select Prefill Question">Select Prefill Question</option> </select>'
            );
            //static prefill selected field is created here
            for (var i = 0; i < getUpdateDispatcherValue.staticPrefills.length; i++) {
                if (getUpdateDispatcherValue.staticPrefills[i].prefillValue === null) {
                    $("#static-select-prefills")
                        .append(`<option questionId="${getUpdateDispatcherValue.staticPrefills[i].questionId}" value="${getUpdateDispatcherValue.staticPrefills[i].note}"> 
            ${getUpdateDispatcherValue.staticPrefills[i].note}
             
           
       </option>`);
                } else {
                    var fieldWrapper = $('<div class="fieldwrapper" id="field">');
                    var sName = `<input type="text" placeholder="Prefill Question" class="fieldname field form__field" disabled questionId="${getUpdateDispatcherValue.staticPrefills[i].questionId}" name="value" value="${getUpdateDispatcherValue.staticPrefills[i].note}" required />`;
                    var fName = $(
                        `<input type="text" placeholder="Static Response"  class="fieldname field1 form__field" name="value" value="${getUpdateDispatcherValue.staticPrefills[i].prefillValue}" required />`
                    );
                    var removeButton = $(
                        `<span class="remove-field" onclick="removeFormfield(event)"><i class="far fa-minus-square"></i></span>`
                    );
                    fieldWrapper.append(sName);
                    fieldWrapper.append(fName);
                    fieldWrapper.append(removeButton);
                    $("#buildyourform1").append(fieldWrapper);
                }
            }
        } else {
            // static prefill lengh is 0 it will disable the static prefill div
            getElement(".static-response").style.display = "none";
        } // only the show default selected field in the vendor detials
         
        
        document.getElementById("icon-block").style.display = "none";
        document.getElementById("configuration-block").style.display = "block";
        
        document.getElementById("getVendorSms").selectedIndex = 0;
        getElement(".SparkPostValueEdited").style.display = "none";
        getElement(".MessageBird").style.display = "none";
        if (data.channelDetails.email.isValid === true) {
            document.getElementById("email-block").style.display = "block";
            if(data.channelDetails.email.vendorname === "SparkPost"){
                document.getElementById("smtpSelect").selectedIndex = 1;
            }
            else{
                document.getElementById("smtpSelect").selectedIndex = 0;
            }
        } else {
            document.getElementById("email-block").style.display = "none";
        }
        if (data.channelDetails.sms.isValid === true) {
            document.getElementById("sms-block").style.display = "block";
            if(data.channelDetails.sms.vendorname === "customSMS"){
                document.getElementById("getVendorSms").selectedIndex = 1;
            }
            else{
                document.getElementById("getVendorSms").selectedIndex = 0;
            }
        } else {
            document.getElementById("sms-block").style.display = "none";
        }
        // get all the object value in the notification object
        var object = data.notify;
        clicks = 0;
        hasNull(object);
        // this function is used to get the all notification value
        function hasNull(target) {
            for (var member in target) {
                if (target[member] == null) {
                    target[member] = "";
                    var data = target[member];
                    document.getElementById(`${member}-notification`).value = data;
                } else {
                    document.getElementById(`${member}-notification`).value =
                        target[member];
                }
            }
            return false;
        }
        if (data.channelDetails.email.isValid === true) {
            // if email isvalid = true  means it will show default CustomSMTP value
            if(data.channelDetails.email.vendorname === "SparkPost"){
                getSparkPostData();
            }
            else{
                getCustomSMTPData();
            }
            
        }
        if (data.channelDetails.sms.isValid === true) {
            // if sms isvalid = true  means it will show default CustomSMTP value
           
            if(data.channelDetails.sms.vendorname === "customSMS"){
                getCustomSMSData();
            }
            else{
                getMessageBirdData();
            }
        }
        getSuperAdminNotificationData();
    });
}

fieldsWithRequiredValidators.forEach(function (x) {
    $(x).focusout(function () {
        required(event.target);
    });
});

fieldsWithEmailValidators.forEach(function (x) {
    $(x).focusout(function () {
        emailFormat(event.target);
    });
});
fieldsWithMultiEmailValidators.forEach(function (x) {
    $(x).focusout(function () {
        notificationEmailValidation(event.target);
    });
});

fieldSuperAdminNotificationValidator.forEach(function (x) {
    $(x).focusout(function () {
        superAdminNotificationEmailValidation(event.target);
    });
});
// on click on the save changes button in Custom SMTP Popup
function vendorEmailUpdateAPI(event) {
    event.preventDefault();
    for (var key in customSmtp) {
        var selector = getElement(customSmtp[key]);
        if (key === "email" && (required(selector) || emailFormat(selector))) {
            //  validators failed
            return;
        } else if (key !== "email" && required(selector)) {
            // validator failed
            return;
        }
    }
    if (!$(".form-error-msg").is(":visible")) {
        // set the Custom SMTP value inside the preview div and hidding the all error message
        document.getElementById(
            "setSenderName"
        ).innerHTML = document.getElementById("getSenderName").value;
        document.getElementById(
            "setEmailAddress"
        ).innerHTML = document.getElementById("getEmailAddress").value;
        document.getElementById(
            "setSmtpServer"
        ).innerHTML = document.getElementById("getSmtpServer").value;
        document.getElementById(
            "setSmtpUsername"
        ).innerHTML = document.getElementById("getSmtpUsername").value;
        document.getElementById("setPort").innerHTML = document.getElementById(
            "getPort"
        ).value;
        document.getElementById("setEnableSsl").innerHTML = document.getElementById(
            "getEnableSsl"
        ).value;
        getElement(".emailEditedValue").style.display = "block";
        document.getElementById("myBtn").innerHTML = "Edit Details For Custom SMTP";
        document.getElementById("error-msg").style.display = "none";
        $(function () {
            // created Customer SMTP object details for Post API
            const object = {
                VendorType: "Email",
                VendorName: document.getElementById("smtpSelect").value,
                IsBulkVendor: false,
                VendorDetails: {},
            };
            var object1 = $("#submitForm").serializeObject();
            object.VendorDetails = Object.assign(object.VendorDetails, object1);
            var settings = {
                // post the created Customer SMTP object to the vendor API
                async: true,
                crossDomain: true,
                url: config.baseURL + "/api/config/vendor",
                method: "POST",

                headers: {
                    "Content-Type": "application/json",
                    Authorization: "Bearer " + auth_token,
                },
                data: JSON.stringify(object),
                error: function (xhr, error) {
                    alert("value is not posted");
                },
            };
            $.ajax(settings).done(function (data) {
                // post Success message
                if (data) {
                    alert("Vendor details saved successfully.");
                }
            });
            return false;
        });

        // Used to enable the scroll and close the SMTP POP-up and enable the save changes button in bottom.
        var modal = document.getElementById("customSmtpOpenPopup");
        modal.style.display = "none";
        enableDisableSaveButton();
        $("body").css({
            overflow: "auto",
        });
    }
}
// on click on the save changes button in Messsage Bird Popup
function venderMessagebird(event) {
    event.preventDefault();
    for (var key in messageBirdVendor) {
        var selector = getElement(messageBirdVendor[key]);
        if (required(selector)) {
            // stop if the validator fails
            return;
        }
    }

    if (!$(".form-error-msg").is(":visible")) {
        document.getElementById(
            "setMessageBird"
        ).innerHTML = document.getElementById("getMessageUrl").value;
        document.getElementById("setAccessKey").innerHTML = document.getElementById(
            "getAccessKey"
        ).value;
        document.getElementById(
            "setOriginator"
        ).innerHTML = document.getElementById("getOriginator").value;
        document.getElementById("setShortCode").innerHTML = document.getElementById(
            "getShortCode"
        ).value;
        document.getElementById("setMultiLanguage").innerHTML = 
        $(
            "#getMultiLanguage option:selected"
        ).text();
        document.getElementById("myBtn1").innerHTML =
            "Edit Details For Message Bird";
        getElement(".MessageBird").style.display = "block";
        document.getElementById("error-msg1").style.display = "none";
        $(function () {
            // Created Message Bird object details for Post API
            const object = {
                VendorType: "Sms",
                VendorName: document.getElementById("getVendorSms").value,
                IsBulkVendor: false,
                VendorDetails: {},
            };
            // post the created Message Bird object to the vendor API
            var object1 = $("#smsmessagebirdForm").serializeObject();
            object.VendorDetails = Object.assign(object.VendorDetails, object1);
            var settings = {
                async: true,
                crossDomain: true,
                url: config.baseURL + "/api/config/vendor",
                method: "POST",

                headers: {
                    "Content-Type": "application/json",
                    Authorization: "Bearer " + auth_token,
                },
                data: JSON.stringify(object),
                error: function (xhr, error) {
                    alert("post email responses is not posted");
                },
            };
            $.ajax(settings).done(function (data) {
                // Post the value to vendor details to the vendor API
                if (data) {
                    alert("Vendor details saved successfully.");
                }
            });
            return false;
        }); // Used to enable the scroll and close the Message Bird and enable the save changes button in bottom.
        var modal = document.getElementById("messageBirdOpenPopup");
        modal.style.display = "none";
        enableDisableSaveButton();
        $("body").css({
            overflow: "auto",
        });
    }
}

function vendorSparkPost(event) {
    event.preventDefault();
    for (var key in sparkPostVendor) {
        var selector = getElement(sparkPostVendor[key]);
        if (key === "email" && (required(selector) || emailFormat(selector))) {
            //  validators failed
            return;
        } else if (key !== "email" && required(selector)) {
            // validator failed
            return;
        }
    }
    if (!$(".form-error-msg").is(":visible")) {
        document.getElementById("setSparkUrl").innerHTML = document.getElementById(
            "getSparkUrl"
        ).value;
        document.getElementById(
            "setSparkApiKey"
        ).innerHTML = document.getElementById("getSparkApiKey").value;
        document.getElementById(
            "setSparkSenderEmail"
        ).innerHTML = document.getElementById("getSparkSenderEmail").value;
        document.getElementById(
            "setSparkSenderName"
        ).innerHTML = document.getElementById("getSparkSenderName").value;
        document.getElementById(
            "setSparkBatchSize"
        ).innerHTML = document.getElementById("getSparkBatchSize").value;
        getElement(".SparkPostValueEdited").style.display = "block";
        document.getElementById("myBtn").innerHTML = "Edit Details For Spark Post";
        document.getElementById("error-msg").style.display = "none";
        $(function () {
            const object = {
                //Creating the object for SparkPost
                VendorType: "Email",
                VendorName: document.getElementById("smtpSelect").value,
                IsBulkVendor: true,
                VendorDetails: {},
            };
            var object1 = $("#smsSparkPostForm").serializeObject();
            object.VendorDetails = Object.assign(object.VendorDetails, object1);
            var settings = {
                // Post the created object to the API
                async: true,
                crossDomain: true,
                url: config.baseURL + "/api/config/vendor",
                method: "POST",

                headers: {
                    "Content-Type": "application/json",
                    Authorization: "Bearer " + auth_token, //passing the token for Authendication
                },
                data: JSON.stringify(object),
                error: function (xhr, error) {
                    alert("post email responses is not posted");
                },
            };
            $.ajax(settings).done(function (data) {
                if (data) {
                    alert("Vendor details saved successfully.");
                }
            });
            return false;
        });
        var modal = document.getElementById("sparkPostOpenPopup"); // hide the opened popup
        modal.style.display = "none";
        enableDisableSaveButton(); // disable or enable the bottom save changes button depends on condition
        $("body").css({
            overflow: "auto",
        });
    }
}
//  custom SMS vendor update API
function vendorSmsUpdateAPI(event) {
    event.preventDefault();

    for (var key in customSms) {
        var selector = getElement(customSms[key]); // Go to required() function for validation
        required(selector);
    }
    if (!$(".form-error-msg").is(":visible")) {
        document.getElementById("setSmsurl").innerHTML = document.getElementById(
            "getSmsurl"
        ).value;
        getElement(".smsEditedValue").style.display = "block";
        document.getElementById("myBtn1").innerHTML = "Edit Details For Custom SMS";
        document.getElementById("error-msg1").style.display = "none";
        $(function () {
            const object = {
                //Creating the object for CustomSMS
                VendorType: "Sms",
                VendorName: document.getElementById("getVendorSms").value,
                IsBulkVendor: false,
                VendorDetails: {},
            };
            var object1 = $("#smsForm").serializeObject();
            object.VendorDetails = Object.assign(object.VendorDetails, object1);
            var settings = {
                async: true,
                crossDomain: true,
                url: config.baseURL + "/api/config/vendor",
                method: "POST",

                headers: {
                    "Content-Type": "application/json",
                    Authorization: "Bearer " + auth_token, //passing the token for Authendication
                },
                data: JSON.stringify(object),
                error: function (xhr, error) {
                    alert("post email responses is not posted");
                },
            };
            $.ajax(settings).done(function (data) {
                if (data) {
                    alert("Vendor details saved successfully.");
                }
            });
            return false;
        });
        var modal = document.getElementById("customSmsOpenPopup"); // hide the opened popup
        modal.style.display = "none";
        enableDisableSaveButton(); // disable or enable the bottom save changes button depends on condition
        $("body").css({
            overflow: "auto",
        });
    }
}

// super admin notification Post API call
function superAdminNotificationUpdateAPI(event) {
    event.preventDefault();

    for (var key in superAdminform) {
        var selector = getElement(superAdminform[key]); // Go to required() function for validation
        superAdminNotificationEmailValidation(selector);
    }
    if (!$(".form-error-msg").is(":visible")) {
        $(function () {
            const object = {
                //Creating the object for super admin notification
                BatchingQueue: "inmemory",
                Sampler: "wxm",
                Unsubscriber: "wxm",
                AccountNotifications: document.getElementById("admin-notification-multi-email").value
            };
            var settings = {
                async: true,
                crossDomain: true,
                url: config.baseURL + "/api/config/extendedproperties",
                method: "POST",

                headers: {
                    "Content-Type": "application/json",
                    Authorization: "Bearer " + auth_token, //passing the token for Authendication
                },
                data: JSON.stringify(object),
                error: function (xhr, error) {
                    alert("post email responses is not posted");
                },
            };
            $.ajax(settings).done(function (data) {
                if (data.AccountNotifications === null || data.AccountNotifications === "") {
                   
                    document.getElementById("setSuperAdminNotificationData").innerHTML = "Super admin notifications are not set up. Please configure it here.";
                }
                else{
                    var value = document.getElementById("admin-notification-multi-email").value
                    addValueSuperAdminPreview(value);
                    alert("Super admin notification details saved successfully.");
                }
            });
            return false;
        });
        var modal = document.getElementById("superAdminNotificationPopup"); // hide the opened popup
        modal.style.display = "none"; // disable or enable the bottom save changes button depends on condition
        $("body").css({
            overflow: "auto",
        });
        
    }
}

// create the Json for vendor detials
$.fn.serializeObject = function () {
    var o = {};
    var a = this.serializeArray();
    $.each(a, function () {
        if (o[this.name] !== undefined) {
            if (!o[this.name].push) {
                o[this.name] = [o[this.name]];
            }
            o[this.name].push(this.value || "");
        } else {
            o[this.name] = this.value || "";
        }
    });
    return o;
};

// On select the email vendor. It show the paritcular email vendor form
function onEmailSelectChange() {
    if (document.getElementById("smtpSelect").value == "CustomSMTP") {
        getCustomSMTPData();
    }
    if (document.getElementById("smtpSelect").value == "SparkPost") {
        getSparkPostData();
    }

    // get New Email Vendor data
    // if (document.getElementById("smtpSelect").value == "newVendorId") {
    //   getNewVendorData();
    // }
}

//On focus on the select Prefill in the Static responses it will show the next static response inputfield
function selectPrefill() {
    $("#buildyourform .fieldwrapper .form__group:last-child").show();
}

// onchange field of sms vendor
function onSelectChangesms() {
    if (document.getElementById("getVendorSms").value == "customSMS") {
        getCustomSMSData();
    }
    if (document.getElementById("getVendorSms").value == "MessageBird") {
        getMessageBirdData();
    }
}

// static response field form creation and generating the

$("#btn-addfield").click(function () {
    if (getUpdateDispatcherValue.staticPrefills.length > clicks) {
        var save = $("#static-select-prefills").val();
        var responseValue = $("#response-value").val();
        if (
            save !== null &&
            responseValue !== "" &&
            save !== "Select Prefill Question"
        ) {
            //removing the error msg and adding the value
            document.getElementById("static-response-error").style.display = "none";
            var attrValue = $("#static-select-prefills option:selected").attr(
                "questionId"
            );
            $(`#static-select-prefills option[value='${save}']`).each(function () {
                $(this).remove();
            });
            var fieldWrapper = $('<div class="fieldwrapper" id="field">');
            // fieldWrapper.data("idx", intId);
            var sName = `<input type="text" placeholder="Prefill Question" class="fieldname field form__field" disabled questionId="${attrValue}" name="value" value="${save}" required />`;
            var fName = $(
                `<input type="text" placeholder="Static Response"  class="fieldname field1 form__field" name="value" value="${responseValue}" required />`
            );
            var removeButton = $(
                `<span onclick="removeFormfield(event)" class="remove-field"><i class="far fa-minus-square"></i></span>`
            );
            responseValue = $("#response-value").val("");
            removeButton.click(function () {
                //removing the placeholder and row of the form while clicking on the remove button
            });

            fieldWrapper.append(sName);
            fieldWrapper.append(fName);
            fieldWrapper.append(removeButton);

            $("#buildyourform1").append(fieldWrapper); // maintaining the click event count

            enableDisableSaveButton();
            $("#buildyourform .fieldwrapper .form__group:last-child").hide();
        } else {
            document.getElementById("static-response-error").style.display = "block"; // disable the form field error msg
            enableDisableSaveButton();
        }
    } else {
        alert(
            `You have only configured ${getUpdateDispatcherValue.staticPrefills.length} prefill in the WXM product, More than that not allowed`
        );
    }
});

function removeFormfield(event) {
    var value = $(event.target).closest(".fieldwrapper").find(".field").val();
    var attrValue = $(event.target)
        .closest(".fieldwrapper")
        .find(".field")
        .attr("questionId");
    $("#static-select-prefills")
        .append(`<option questionId="${attrValue}" value="${value}"> 
    ${value} 
</option>`);
    responseValue = $("#response-value").val("");
    document.getElementById("static-response-error").style.display = "none";

    $(event.target).closest(".fieldwrapper").remove();
    enableDisableSaveButton();
}

function validateEmail(email) {
    // multi email validation for form field
    var re = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(String(email));
}

const validate = (emails) => {
    // multi email validation for form field
    emails = emails.split(/[;]/).map((x) => x.trim().toLocaleLowerCase());
    if (emails.length !== new Set(emails).size) return false;
    return emails.every(validateEmail);
};

//notification validation and JSON creation
function saveChanges(event) {
    event.preventDefault();

    for (var key in notificationForm) {
        var selector = getElement(notificationForm[key]);
        notificationEmailValidation(selector);  
  }
  if(!$('.notification-error').is(':visible')) {
        var divchildlength = $("#buildyourform1").children().length;
        staffPrefillArray = [];
        for (var j = 0; j < divchildlength; j++) {
            // JSON creating for added static responses in the static reponse block
            var selectValue = $(
                `#buildyourform1 div:nth-child(${j + 1}) .field`
            ).val();
            var textValue = $(
                `#buildyourform1 div:nth-child(${j + 1}) .field1`
            ).val();
            var selectAttrValue = $(
                `#buildyourform1 div:nth-child(${j + 1}) .field`
            ).attr("questionId");
            var generateJsonvalue = {
                questionId: selectAttrValue,
                note: selectValue,
                prefillValue: textValue,
            };
            staffPrefillArray.push(generateJsonvalue);
        }// JSON creating for notification step up form
        prefillArrayValue = getUpdateDispatcherValue;
        for(var j = 0; j < prefillArrayValue.staticPrefills.length; j++){
            prefillArrayValue.staticPrefills[j].prefillValue = null;
  
        }
        var array1 = [];
            array1 = prefillArrayValue.staticPrefills;   // storing the staff-prefill values
        const combinedArray = array1.map((o) =>
            Object.assign(
                o,
                staffPrefillArray.find((a) => a.questionId === o.questionId)
            )
        );
        getUpdateDispatcherValue.staticPrefills = combinedArray; // combine both static responses and notification value
        getUpdateDispatcherValue.notify.d = document.getElementById("d-notification").value;
        getUpdateDispatcherValue.notify.i = document.getElementById("i-notification").value;
        getUpdateDispatcherValue.notify.w = document.getElementById("w-notification").value;
        getUpdateDispatcherValue.notify.e = document.getElementById("e-notification").value;
        getUpdateDispatcherValue.notify.f = document.getElementById("f-notification").value;
        if (getUpdateDispatcherValue.channelDetails.email.isValid == true) {
            // passing the email selected value here
            getUpdateDispatcherValue.channelDetails.email.vendorname = document.getElementById(
                "smtpSelect"
            ).value;
        }
        if (getUpdateDispatcherValue.channelDetails.sms.isValid == true) {
            getUpdateDispatcherValue.channelDetails.sms.vendorname = document.getElementById(
                "getVendorSms"
            ).value;
        }

        var settings = {
            // post the created JSON to the updatedispatch API
            async: true,
            crossDomain: true,
            url: config.baseURL + "/api/config/dispatch",
            method: "POST",

            headers: {
                "Content-Type": "application/json",
                Authorization: "Bearer " + auth_token,
            },
            data: JSON.stringify(getUpdateDispatcherValue),
           
            error: function (xhr, error) {
                alert("Dispatches updation is unsuccessful");
            },
        };
        $.ajax(settings).done(function (data) {
            if (data) {
                alert("Settings saved successfully.");
            }
        });
    }
}

// Get the value from SparK post API
function getSparkPostData() {
    var settings1 = {
        async: true,
        crossDomain: true,
        url: config.baseURL + "/api/config/vendor/SparkPost",
        method: "GET",
        headers: {
            Authorization: "Bearer " + auth_token,
        },
        statusCode: {
            // When no data presented this block will excecute
            204: function () {
                getElement(".emailEditedValue").style.display = "none";
                document.getElementById("myBtn").innerHTML =
                    "Add Details For Spark Post";
                document.getElementById("error-msg").style.display = "block";

                document.getElementById("error-msg").innerHTML =
                    "Spark Post settings missing. Please add details";
                enableDisableSaveButton();
            },
        },

        error: function (xhr, error) {
            alert("API error"); // show the API error here
        },
    };
    $.ajax(settings1).done(function (oResponse) {
        if (oResponse) {
            document.getElementById("error-msg").style.display = "none";
            //display the API data in both sparkpost form and sparkpost preview
            document.getElementById("myBtn").innerHTML =
                "Edit Details For Spark Post";
            getVendorByName = oResponse.vendorDetails;
            getElement(".emailEditedValue").style.display = "none";
            document.getElementById("setSparkUrl").innerHTML = getVendorByName.Url;
            document.getElementById("setSparkApiKey").innerHTML =
                getVendorByName.ApiKey;
            document.getElementById("setSparkSenderEmail").innerHTML =
                getVendorByName.SenderEmail;
            document.getElementById("setSparkSenderName").innerHTML =
                getVendorByName.SenderName;
            document.getElementById("setSparkBatchSize").innerHTML =
                getVendorByName.BatchSize;
            document.getElementById("getSparkUrl").value = getVendorByName.Url;
            document.getElementById("getSparkApiKey").value = getVendorByName.ApiKey;
            document.getElementById("getSparkSenderEmail").value =
                getVendorByName.SenderEmail;
            document.getElementById("getSparkSenderName").value =
                getVendorByName.SenderName;
            document.getElementById("getSparkBatchSize").value =
                getVendorByName.BatchSize;
            getElement(".SparkPostValueEdited").style.display = "block";
            enableDisableSaveButton();
        }
    });
}

// get customSMS vendor name properties
function getCustomSMSData() {
    var settings = {
        async: true,
        crossDomain: true,
        url: config.baseURL + "/api/config/vendor/customSMS",
        method: "GET",
        headers: {
            Authorization: "Bearer " + auth_token, // Passing the auth token here
        },
        statusCode: {
            204: function () {
                // When no data present in API this block will excecute
                getElement(".smsEditedValue").style.display = "none";
                document.getElementById("myBtn1").innerHTML =
                    "Add Details For Custom SMS";
                document.getElementById("error-msg1").style.display = "block";
                getElement(".MessageBird").style.display = "none";
                document.getElementById("error-msg1").innerHTML =
                    "Custom SMS settings missing. Please add details";
                enableDisableSaveButton();
            },
        },

        error: function (xhr, error) {
            //display the API Error here
            alert("API error");
        },
    };
    $.ajax(settings).done(function (oResponse) {
        if (oResponse) {
            //display the API data in both Custom SMS form and Custom SMS preview
            document.getElementById("error-msg1").style.display = "none";
            getElement(".MessageBird").style.display = "none";
            document.getElementById("myBtn1").innerHTML =
                "Edit Details For Custom SMS";
            getVendorByName = oResponse.vendorDetails;
            getElement(".smsEditedValue").style.display = "block";
            document.getElementById("setSmsurl").innerHTML = getVendorByName.Url;
            document.getElementById("getSmsurl").value = getVendorByName.Url;
            enableDisableSaveButton();
        }
    });
}
// get customSMTP vendor name propertie
function getCustomSMTPData() {
    var settings = {
        async: true,
        crossDomain: true,
        url: config.baseURL + "/api/config/vendor/CustomSMTP",
        method: "GET",
        headers: {
            Authorization: "Bearer " + auth_token,
        },
        statusCode: {
            204: function () {
                // When no data present in API this block will excecute
                getElement(".emailEditedValue").style.display = "none";
                document.getElementById("myBtn").innerHTML =
                    "Add Details For Custom SMTP";
                document.getElementById("error-msg").style.display = "block";
                document.getElementById("error-msg").innerHTML =
                    "Custom SMTP settings missing. Please add details";
                enableDisableSaveButton();
            },
        },

        error: function (xhr, error) {
            //display the API msg Error here
            alert("API error");
        },
    };
    $.ajax(settings).done(function (oResponse) {
        if (oResponse) {
            //display the API data in both customSMTP form and sparkpost customSMTP
            document.getElementById("error-msg").style.display = "none";
            getElement(".SparkPostValueEdited").style.display = "none";
            document.getElementById("myBtn").innerHTML =
                "Edit Details For Custom SMTP";
            getVendorByName = oResponse.vendorDetails;
            getElement(".emailEditedValue").style.display = "block";
            document.getElementById("setSenderName").innerHTML =
                getVendorByName.SenderName;
            document.getElementById("setEmailAddress").innerHTML =
                getVendorByName.SenderAddress;
            document.getElementById("setSmtpServer").innerHTML =
                getVendorByName.SmtpServer;
            document.getElementById("setSmtpUsername").innerHTML =
                getVendorByName.SmtpUsername;
            document.getElementById("setPort").innerHTML = getVendorByName.Port;
            document.getElementById("setEnableSsl").innerHTML = getVendorByName.SSL;
            document.getElementById("getSenderName").value =
                getVendorByName.SenderName;
            document.getElementById("getEmailAddress").value =
                getVendorByName.SenderAddress;
            document.getElementById("getSmtpServer").value =
                getVendorByName.SmtpServer;
            document.getElementById("getSmtpUsername").value =
                getVendorByName.SmtpUsername;
            document.getElementById("getSmtpPassword").value =
                getVendorByName.SmtpPassword;
            document.getElementById("getPort").value = getVendorByName.Port;
            document.getElementById("getEnableSsl").value = getVendorByName.SSL;
            enableDisableSaveButton(); // disable or enable the bottom save changes button depends on condition
        }
    });
}

function getMessageBirdData() {
    var settings1 = {
        async: true,
        crossDomain: true,
        url: config.baseURL + "/api/config/vendor/MessageBird",
        method: "GET",
        headers: {
            // When no data present this block will excecute
            Authorization: "Bearer " + auth_token,
        },
        statusCode: {
            204: function () {
                // When no data present in API this block will excecute
                getElement(".smsEditedValue").style.display = "none";
                document.getElementById("myBtn1").innerHTML =
                    "Add Details For Message Bird";
                document.getElementById("error-msg1").style.display = "block";
                document.getElementById("error-msg1").innerHTML =
                    "Message Bird settings missing. Please add details";
                enableDisableSaveButton();
            },
        },

        error: function (xhr, error) {
            //display the API msg Error here
            alert("API error");
        },
    };
    $.ajax(settings1).done(function (oResponse) {
        if (oResponse) {
            //display the API data in both Message Bird form and Message Bird customSMTP
            document.getElementById("error-msg1").style.display = "none";
            getVendorByName = oResponse.vendorDetails;
            getElement(".smsEditedValue").style.display = "none";
            document.getElementById("myBtn1").innerHTML =
                "Edit Details For Message Bird";
            document.getElementById("setMessageBird").innerHTML = getVendorByName.Url;
            document.getElementById("setAccessKey").innerHTML =
                getVendorByName.AccessKey;
            document.getElementById("setOriginator").innerHTML =
                getVendorByName.Originator;
            document.getElementById("setShortCode").innerHTML =
                getVendorByName.ShortCode;
                document.getElementById("setMultiLanguage").innerHTML = getVendorByName.DataCoding;
            document.getElementById("getMessageUrl").value = getVendorByName.Url;
            document.getElementById("getAccessKey").value = getVendorByName.AccessKey;
            document.getElementById("getOriginator").value =
                getVendorByName.Originator;
                document.getElementById("getMultiLanguage").value = getVendorByName.DataCoding;
            document.getElementById("getShortCode").value = getVendorByName.ShortCode;
            getElement(".MessageBird").style.display = "block";
            enableDisableSaveButton(); // disable or enable the bottom save changes button depends on condition
        }
    });
}

function enableDisableSaveButton() {
    // function for disable or enable the bottom save changes button depends on condition
    if (
        $("#error-msg").is(":visible") ||
        $("#error-msg1").is(":visible") ||
        $("#static-response-error").is(":visible") ||
        $(".error-noti").is(":visible")
    ) {
        getElement(".error-save-vendor").style.display = "block";
        $("#generate-dispatcher").attr("disabled", true);
    } else {
        getElement(".error-save-vendor").style.display = "none";
        $("#generate-dispatcher").attr("disabled", false);
    }
}


//get Super Admin notification API 
function getSuperAdminNotificationData() {
    var settings = {
        async: true,
        crossDomain: true,
        url: config.baseURL + "/api/config/extendedproperties",
        method: "GET",
        headers: {
            Authorization: "Bearer " + auth_token, // Passing the auth token here
        },

        error: function (xhr, error) {
            //display the API Error here
            alert("API error");
        },
    };
    $.ajax(settings).done(function (oResponse) {
        if (oResponse.AccountNotifications === null || oResponse.AccountNotifications === "" ) {
         document.getElementById("setSuperAdminNotificationData").innerHTML = "Super admin notifications are not set up. Please configure it here.";
        
         document.getElementById("admin-notification-multi-email").value = "";
        }
        else{
            var values = oResponse.AccountNotifications;
            addValueSuperAdminPreview(values);
            
            document.getElementById("admin-notification-multi-email").value = values;
        }
    });
}

function addValueSuperAdminPreview(value){
    var res = value.replace(/;/g, ", ");
    document.getElementById("setSuperAdminNotificationData").innerHTML = "Super admin notifications will be sent to " + res;
}

//remove Token
function logout(){
sessionStorage.removeItem("Oauth_Token");
}