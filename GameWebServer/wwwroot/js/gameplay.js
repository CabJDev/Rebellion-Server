window.onload = function () {
    window.setTimeout(GetRoleInfo, 100);
    window.setTimeout(GetPlayerNames, 100);
    for (var i = 0; i < 15; i++) {
        document.getElementById("player" + (i + 1) + "Name").innerText = "";

        var button = document.getElementById("player" + (i + 1) + "Button")
        button.disabled = true;
        button.style.opacity = "0";

        document.getElementById("p" + (i + 1)).style.opacity = "0";
    }
}

// Button events
document.getElementById("player1Button").addEventListener("click", function () {
    document.getElementById("player1Button").innerText = "X";
    console.log("Button 1 clicked!");
})
document.getElementById("player2Button").addEventListener("click", function () {
    document.getElementById("player2Button").innerText = "X";
    console.log("Button 2 clicked!");
})
document.getElementById("player3Button").addEventListener("click", function () {
    document.getElementById("player3Button").innerText = "X";
    console.log("Button 3 clicked!");
})
document.getElementById("player4Button").addEventListener("click", function () {
    document.getElementById("player4Button").innerText = "X";
    console.log("Button 4 clicked!");
})
document.getElementById("player5Button").addEventListener("click", function () {
    document.getElementById("player5Button").innerText = "X";
    console.log("Button 5 clicked!");
})
document.getElementById("player6Button").addEventListener("click", function () {
    document.getElementById("player6Button").innerText = "X";
    console.log("Button 6 clicked!");
})
document.getElementById("player7Button").addEventListener("click", function () {
    document.getElementById("player7Button").innerText = "X";
    console.log("Button 7 clicked!");
})
document.getElementById("player8Button").addEventListener("click", function () {
    document.getElementById("player8Button").innerText = "X";
    console.log("Button 8 clicked!");
})
document.getElementById("player9Button").addEventListener("click", function () {
    document.getElementById("player9Button").innerText = "X";
    console.log("Button 9 clicked!");
})
document.getElementById("player10Button").addEventListener("click", function () {
    document.getElementById("player10Button").innerText = "X";
    console.log("Button 10 clicked!");
})
document.getElementById("player11Button").addEventListener("click", function () {
    document.getElementById("player11Button").innerText = "X";
    console.log("Button 11 clicked!");
})
document.getElementById("player12Button").addEventListener("click", function () {
    document.getElementById("player12Button").innerText = "X";
    console.log("Button 12 clicked!");
})
document.getElementById("player13Button").addEventListener("click", function () {
    document.getElementById("player13Button").innerText = "X";
    console.log("Button 13 clicked!");
})
document.getElementById("player14Button").addEventListener("click", function () {
    document.getElementById("player14Button").innerText = "X";
    console.log("Button 14 clicked!");
})
document.getElementById("player15Button").addEventListener("click", function () {
    document.getElementById("player15Button").innerText = "X";
    console.log("Button 15 clicked!");
})
// Button events

function GetRoleInfo() {
    connection.invoke("RetrieveRoleInfo", document.cookie).catch(function (err) {
        return console.error(err.toString());
    });
}

function GetPlayerNames() {
    connection.invoke("RetrievePlayerNames", document.cookie).catch(function (err) {
        return console.error(err.toString());
    });
}

connection.on("GetRoles", (name, role, winCon) => {
    document.getElementById("roleName").innerText = name;
    document.getElementById("roleDesc").innerText = role;
    document.getElementById("winConDesc").innerText = winCon;
})

connection.on("GetNames", (names) => {
    for (var i = 0; i < 15; i++) {
        if (names[i] != "") {
            document.getElementById("player" + (i + 1) + "Name").innerText = names[i];
            document.getElementById("p" + (i + 1)).style.opacity = "1";
        }
    }
})

connection.on("EnableButtons", (toEnable) => {
    for (var i = 0; i < 15; i++) {
        if (toEnable[i] == 1) {
            var button = document.getElementById("player" + (i + 1) + "Button")
            button.disabled = false;
            button.style.opacity = "1";
        }
    }
})