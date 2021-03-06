﻿function panesVisible(visible) {
    initTabMenu(visible);
}

function scrollToEnd() {
    var scrollTo = beginningOfCurrentTurnScrollPosition - 15;
    var currentScrollTop = $("body").scrollTop();
    if (scrollTo > currentScrollTop) {
        var maxScrollTop = $(document).height() - $(window).height();
        if (scrollTo > maxScrollTop) scrollTo = maxScrollTop;
        var distance = scrollTo - currentScrollTop;
        var duration = distance / 0.4;
        $("body,html").stop().animate({ scrollTop: scrollTo }, duration, "easeInOutCubic");
    }
    $("#txtCommand").focus();
}

function setBackground(col) {
    $("#gameBorder").css("background-color", col);
    $("#txtCommandDiv").css("background-color", col);
    $("body").css("background-color", col);
}

function setPanelHeight() {
}

function setPanelContents(html) {
}

function setCommandBarStyle() {
}

var resizeTimer;

function ui_init() {
    //    resizeUI();
    //    $(window).resize(function () {
    //        clearTimeout(resizeTimer);
    //        resizeTimer = setTimeout(function () {
    //            resizeUI();
    //        }, 100);
    //    });
    //    document.addEventListener("orientationChanged", resizeUI);

    SetMenuFontSize("20px");
    _allowMenuFontSizeChange = false;

    initTabMenu(true);

    $("button.backButton span").html("&lt; Back to game");
    $("button.backButton").click(function () { tabSelected('game'); });
    $("#cmdLook").click(function () { sendCommand("look"); });
    $("#cmdRestart").click(function () { window.location.reload(); });
    $("#cmdUndo").click(function () { sendCommand("undo"); });
    $("#cmdWait").click(function () { sendCommand("wait"); });

    // fix to make compass button icons centred
    $(".compassbutton span").css("left", "1.5em");
}

function initTabMenu(full) {
    var options;

    if (full) {
        options = [
            { title: "Inventory", action: { type: "fn", callback: function() { tabSelected('inventory'); } } },
            { title: "Location", action: { type: "fn", callback: function () { tabSelected('objects'); } } },
            { title: "Exits", action: { type: "fn", callback: function() { tabSelected('exits'); } } },
            { title: "More", action: { type: "fn", callback: function() { tabSelected('more'); } } }
        ];
    }
    else {
        options = [
            { title: "More", action: { type: "fn", callback: function () { tabSelected('more'); } } }
        ];
    }

    $("#tabButton").jjmenu(options);
}

function resizeUI() {
}

function beginWait() {
    _waitMode = true;
    $("#inputBar").fadeOut(400, function () {
        if (_waitMode) {
            $("#endWaitLink").fadeTo(400, 1);
        }
    });
    markScrollPosition();
}

function endWait() {
    if (!_waitMode) return;
    _waitMode = false;
    window.setTimeout(function () {
        $("#fldUIMsg").val("endwait");
        $("#cmdSubmit").click();
    }, 100);
    window.setTimeout(function () {
        if (!_waitMode) {
            $("#endWaitLink").fadeOut(400, function () {
                if (!_waitMode) {
                    $("#inputBar").fadeTo(400, 1);
                }
            });
        }
    }, 200);
}

function sessionTimeout() {
    disableInterface();
    $("#sessionTimeoutDiv").show();
}

function gameFinished() {
    disableInterface();
    $("#gameOverDiv").show();
}

function disableInterface() {
    $("#txtCommandDiv").hide();
    $("#gamePanesRunning").hide();
}

var currentTab = "game";

function tabSelected(tab) {
    if (tab != currentTab) {
        var olddiv = divNameForTab(currentTab);
        var newdiv = divNameForTab(tab);
        currentTab = tab;
        if (tab != "game") {
            $("#gameContent").css("visibility", "hidden");
        }
        newdiv.show();
        if (tab == "game") {
            $('html, body').animate({ scrollTop: $(document).height() }, 10);
        }
        else {
            $('html, body').animate({ scrollTop: 0 }, 10);
        }
        olddiv.hide();
        $("#gameOptions").hide();
        if (tab == "game") {
            setTimeout(function () {
                $("#gameContent").css("visibility", "visible");
            }, 50);
        }
    }
}

function divNameForTab(tab) {
    switch (tab) {
        case "game":
            return $("#gameContent");
        case "inventory":
            return $("#gamePanes");
        case "objects":
            return $("#gameObjects");
        case "exits":
            return $("#gameExits");
        case "more":
            return $("#gameMore");
    }
}

function setInterfaceString(name, text) {
    switch (name) {
        case "InventoryLabel":
            $("#inventoryLabel").html(text);
            break;
        case "StatusLabel":
            $("#statusVarsLabel").html(text);
            break;
        case "PlacesObjectsLabel":
            // not implemented on mobile WebPlayer
            break;
        case "CompassLabel":
            $("#compassLabel").html(text);
            break;
        case "InButtonLabel":
            $("#cmdCompassIn span").html(text);
            break;
        case "OutButtonLabel":
            $("#cmdCompassOut span").html(text);
            break;
        case "EmptyListLabel":
            break;
        case "NothingSelectedLabel":
            break;
        case "TypeHereLabel":
            $("#txtCommand").prop("placeholder", text);
            break;
        case "ContinueLabel":
            $("#endWaitLink").html(text);
            break;
    }
}

function updateLocation(text) {
    $("#placesObjectsLabel").html(text);
}

function afterSendCommand() {
    tabSelected("game");
}

function afterSave() {
    tabSelected("game");
}

var lastPaneLinkId = 0;

function updateList(listName, listData) {
    var listElement = "";
    var emptyListLabel = "";

    if (listName == "inventory") {
        listElement = "#inventoryList";
        emptyListLabel = "#inventoryEmpty";
    }

    if (listName == "placesobjects") {
        listElement = "#objectsList";
        emptyListLabel = "#placesObjectsEmpty";
    }

    $(listElement).empty();
    $(listElement).show();
    var listcount = 0;
    var anyItem = false;

    $.each(listData, function (key, value) {
        var data = JSON.parse(value);
        var objectDisplayName = data["Text"];
        var objectVerbs = data["Verbs"].join("/");

        if (listName == "inventory" || $.inArray(objectDisplayName, _compassDirs) == -1) {
            listcount++;
            lastPaneLinkId++;
            var paneLinkId = "paneLink" + lastPaneLinkId;
            $(listElement).append(
                "<li id=\"" + paneLinkId + "\" href=\"#\">" + objectDisplayName + "</li>"
            );
            $("#" + paneLinkId).bind("touchstart", function () {
                $(this).addClass("elementListHover")
            });
            $("#" + paneLinkId).bind("touchend", function () {
                $(this).removeClass("elementListHover")
            });
            bindMenu(paneLinkId, objectVerbs, data["ElementName"], data["ElementId"]);
            anyItem = true;
        }
    });
    $(listElement + " li:last-child").addClass('last-child')
    if (listcount == 0) $(listElement).hide();
    if (anyItem) {
        $(emptyListLabel).hide();
    }
    else {
        $(emptyListLabel).show();
    }
}

var _currentPlayer = "";

function playWav(filename, sync, looped) {
    playAudio(filename, "wav", sync, looped);
}

function playMp3(filename, sync, looped) {
    playAudio(filename, "mp3", sync, looped);
}

function playAudio(filename, format, sync, looped) {
    if (sync) {
        finishSync();
    }
}

function stopAudio() {
}

function finishSync() {
    window.setTimeout(function () {
        $("#txtCommandDiv").show();
        $("#fldUIMsg").val("endwait");
        $("#cmdSubmit").click();
    }, 100);
}

function setGameWidth() {
}