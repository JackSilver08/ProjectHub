(() => {
    const projectIdInput = document.getElementById("projectId");
    if (!projectIdInput?.value) return;

    const projectId = projectIdInput.value;

    let taskStatusChart = null;
    let taskLineChart = null;
    let statusChartType = "doughnut";
    let lastStatusData = null;
    let currentRange = "30d";

    function buildStatusChart(type, dataArr) {
        const ctx = document.getElementById("taskStatusChart");
        if (!ctx) return;

        if (taskStatusChart) taskStatusChart.destroy();

        taskStatusChart = new Chart(ctx, {
            type,
            data: {
                labels: ["Todo", "In Progress", "Review", "Completed"],
                datasets: [{
                    data: dataArr,
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: "bottom" } },
                cutout: type === "doughnut" ? "70%" : undefined
            }
        });
    }

    async function fetchSummaryStats() {
        const url = new URL(window.location.href);
        url.searchParams.set("handler", "SummaryStats");
        const res = await fetch(url.toString(), { headers: { "Accept": "application/json" } });
        if (!res.ok) throw new Error("SummaryStats fetch failed");
        return res.json();
    }

    async function fetchTimeline(range) {
        const url = new URL(window.location.href);
        url.searchParams.set("handler", "Timeline");
        url.searchParams.set("range", range);
        const res = await fetch(url.toString(), { headers: { "Accept": "application/json" } });
        if (!res.ok) throw new Error("Timeline fetch failed");
        return res.json();
    }

    function updateSummaryUI(summary) {
        document.getElementById("totalTasks").innerText = summary.totalTasks;
        document.getElementById("openTasks").innerText = summary.openTasks;
        document.getElementById("completedTasks").innerText = summary.completedTasks;
    }

    function updateStatusChart(chart) {
        lastStatusData = [chart.todo, chart.inProgress, chart.review, chart.completed];

        if (!taskStatusChart) {
            buildStatusChart(statusChartType, lastStatusData);
            return;
        }
        taskStatusChart.data.datasets[0].data = lastStatusData;
        taskStatusChart.update();
    }

    async function renderLineChart(range) {
        const ctx = document.getElementById("taskLineChart");
        if (!ctx) return;

        const data = await fetchTimeline(range);

        if (!taskLineChart) {
            taskLineChart = new Chart(ctx, {
                type: "line",
                data: {
                    labels: data.labels,
                    datasets: [
                        { label: "Created", data: data.created, tension: 0.35 },
                        { label: "Completed", data: data.completed, tension: 0.35 }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { position: "bottom" } }
                }
            });
        } else {
            taskLineChart.data.labels = data.labels;
            taskLineChart.data.datasets[0].data = data.created;
            taskLineChart.data.datasets[1].data = data.completed;
            taskLineChart.update();
        }
    }

    // expose cho onclick trong Razor
    window.toggleStatusChart = (type) => {
        statusChartType = type;
        buildStatusChart(type, lastStatusData ?? [0, 0, 0, 0]);
    };

    window.setLineRange = async (range) => {
        currentRange = range;
        await renderLineChart(range);
    };

    window.refreshSummary = async () => {
        const s = await fetchSummaryStats();
        updateSummaryUI(s.summary);
        updateStatusChart(s.chart);
        await renderLineChart(currentRange);
    };

    // init lần đầu
    document.addEventListener("DOMContentLoaded", async () => {
        const s = await fetchSummaryStats();
        updateSummaryUI(s.summary);
        updateStatusChart(s.chart);

        await renderLineChart(currentRange);
    });

    // ====== SignalR giữ như cũ, chỉ bổ sung update line nếu muốn ======
    if (typeof signalR === "undefined") return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/project")
        .withAutomaticReconnect()
        .build();

    connection.on("ProjectStatsUpdated", async (data) => {
        updateSummaryUI(data.summary);
        updateStatusChart(data.chart);

        // optional: refresh line chart khi có update
        // await renderLineChart(currentRange);
    });

    connection.start()
        .then(() => connection.invoke("JoinProject", projectId))
        .catch(console.error);
})();