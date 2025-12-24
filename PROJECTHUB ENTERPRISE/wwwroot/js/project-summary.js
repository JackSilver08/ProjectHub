(() => {

    // =========================
    // GET PROJECT ID
    // =========================
    const projectIdInput = document.getElementById("projectId");

    if (!projectIdInput || !projectIdInput.value) {
        console.warn("[ProjectSummary] projectId not found, skip SignalR");
        return; // ⛔ Không phải trang Project Details
    }

    const projectId = projectIdInput.value;

    // =========================
    // CHECK SIGNALR
    // =========================
    if (typeof signalR === "undefined") {
        console.error("[ProjectSummary] signalR not loaded");
        return;
    }

    // =========================
    // BUILD CONNECTION
    // =========================
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/project")
        .withAutomaticReconnect()
        .build();

    // =========================
    // LISTEN EVENTS
    // =========================
    connection.on("ProjectStatsUpdated", data => {

        // ===== SUMMARY =====
        document.getElementById("totalTasks").innerText =
            data.summary.totalTasks;

        document.getElementById("openTasks").innerText =
            data.summary.openTasks;

        document.getElementById("completedTasks").innerText =
            data.summary.completedTasks;

        // ===== CHART =====
        if (taskStatusChart) {
            taskStatusChart.data.datasets[0].data = [
                data.chart.todo,
                data.chart.inProgress,
                data.chart.review,
                data.chart.completed
            ];

            taskStatusChart.update();
        }
    });


    // =========================
    // CONNECT
    // =========================
    connection.start()
        .then(() => {
            console.log("[SignalR] Connected");
            return connection.invoke("JoinProject", projectId);
        })
        .catch(err => {
            console.error("[SignalR] Connection error", err);
        });

})();