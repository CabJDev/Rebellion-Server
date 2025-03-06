function backButtonPressed() {
    connection.invoke("UserDisconnect").catch(function (err) {
        return console.error(err.toString());
    });
}