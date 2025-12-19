(() => {

    const projectIdInput = document.getElementById("projectId");
    if (!projectIdInput) {
        console.warn("projectId not found");
        return; // ✅ hợp lệ
    }

    const projectId = projectIdInput.value;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/project")
        .withAutomaticReconnect()
        .build();

    connection.on("SummaryUpdated", data => {
        console.log("Realtime summary", data);
    });

    connection.start()
        .then(() => {
            console.log("SignalR connected");
            connection.invoke("JoinProject", projectId);
        })
        .catch(err => console.error(err));

})();