// PROJECT DETAILS - ALL FUNCTIONALITIES
// =====================================

// GLOBAL VARIABLES
let selectedUserId = null;
let currentTaskIdForMention = null;
let currentTextareaForMention = null;

// =========================
// MEMBER MANAGEMENT
// =========================
function initializeMemberManagement() {
    const emailInput = document.getElementById('memberEmail');
    const resultList = document.getElementById('userSearchResult');
    const selectedBox = document.getElementById('selectedUser');
    const addBtn = document.getElementById('addMemberBtn');

    if (!emailInput || !addBtn) return;

    // SEARCH USER
    emailInput.addEventListener('input', async function () {
        const email = this.value.trim();

        if (email.length < 3) {
            if (resultList) resultList.classList.add('d-none');
            return;
        }

        try {
            const res = await fetch('/api/users/search?email=' + encodeURIComponent(email));
            if (!res.ok) return;

            const users = await res.json();

            if (resultList) {
                resultList.innerHTML = '';
                resultList.classList.remove('d-none');

                users.forEach(function (u) {
                    const li = document.createElement('li');
                    li.className = 'list-group-item list-group-item-action';
                    li.innerHTML = '<strong>' + u.fullName + '</strong><br><small>' + u.email + '</small>';

                    li.onclick = function () {
                        selectedUserId = u.id;

                        if (selectedBox) {
                            selectedBox.innerHTML =
                                '<strong>' + u.fullName + '</strong><br><small>' + u.email + '</small>';
                            selectedBox.classList.remove('d-none');
                        }

                        if (resultList) resultList.classList.add('d-none');
                        if (addBtn) addBtn.disabled = false;
                    };

                    resultList.appendChild(li);
                });
            }
        } catch (error) {
            console.error('Error searching users:', error);
        }
    });

    // ADD MEMBER
    addBtn.addEventListener('click', async function () {
        if (!selectedUserId) {
            alert("Please select a user");
            return;
        }

        const projectId = document.getElementById('projectId') ? document.getElementById('projectId').value : null;
        if (!projectId) {
            alert("Project ID not found");
            return;
        }

        try {
            const res = await fetch("?handler=AddMember", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken":
                        document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    projectId: projectId,
                    userId: selectedUserId
                })
            });

            if (res.ok) {
                location.reload();
            } else {
                alert("Failed to add member");
            }
        } catch (error) {
            console.error('Error adding member:', error);
            alert("Error adding member");
        }
    });
}

function openRemoveModal(projectId, userId, fullName) {
    const removeMemberName = document.getElementById("removeMemberName");
    if (removeMemberName) {
        removeMemberName.innerText = fullName;
    }

    const form = document.getElementById("removeMemberForm");
    if (form) {
        form.action = '?handler=RemoveMember&projectId=' + projectId + '&userId=' + userId;
    }

    const modalElement = document.getElementById("confirmRemoveModal");
    if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

// =========================
// TASK MANAGEMENT
// =========================
function initializeTaskManagement() {
    const createTaskBtn = document.getElementById("createTaskBtn");
    if (createTaskBtn) {
        createTaskBtn.addEventListener("click", async function () {
            const title = document.getElementById("taskTitle") ? document.getElementById("taskTitle").value.trim() : '';
            const projectId = document.getElementById("taskProjectId") ? document.getElementById("taskProjectId").value : null;

            if (!title) {
                alert("Title is required");
                return;
            }

            if (!projectId) {
                alert("Project ID not found");
                return;
            }

            try {
                const res = await fetch("?handler=CreateTask", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken":
                            document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify({
                        projectId: projectId,
                        title: title,
                        assigneeId: document.getElementById("taskAssignee") ? document.getElementById("taskAssignee").value || null : null,
                        deadline: document.getElementById("taskDeadline") ? document.getElementById("taskDeadline").value || null : null
                    })
                });

                if (res.ok) {
                    location.reload();
                } else {
                    alert("Create task failed");
                }
            } catch (error) {
                console.error('Error creating task:', error);
                alert("Error creating task");
            }
        });
    }
}

function openEditTask(id, title, assigneeId, deadline, status) {
    document.getElementById("editTaskId").value = id;
    document.getElementById("editTaskTitle").value = title;
    document.getElementById("editTaskAssignee").value = assigneeId || "";
    document.getElementById("editTaskDeadline").value = deadline || "";
    document.getElementById("editTaskStatus").value = status;

    const modalElement = document.getElementById("editTaskModal");
    if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

async function saveTaskEdit() {
    const taskId = document.getElementById("editTaskId") ? document.getElementById("editTaskId").value : null;
    if (!taskId) {
        alert("Task ID not found");
        return;
    }

    try {
        const res = await fetch("?handler=EditTask", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken":
                    document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                taskId: taskId,
                title: document.getElementById("editTaskTitle") ? document.getElementById("editTaskTitle").value : '',
                assigneeId: document.getElementById("editTaskAssignee") ? document.getElementById("editTaskAssignee").value || null : null,
                deadline: document.getElementById("editTaskDeadline") ? document.getElementById("editTaskDeadline").value || null : null,
                status: document.getElementById("editTaskStatus") ? parseInt(document.getElementById("editTaskStatus").value) : 0
            })
        });

        if (res.ok) {
            location.reload();
        } else {
            alert("Update failed");
        }
    } catch (error) {
        console.error('Error saving task edit:', error);
        alert("Error updating task");
    }
}

async function changeTaskStatus(taskId, status) {
    try {
        const res = await fetch("?handler=ChangeTaskStatus", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken":
                    document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                taskId: taskId,
                status: status
            })
        });

        if (res.ok) {
            location.reload();
        } else {
            alert("Change status failed");
        }
    } catch (error) {
        console.error('Error changing task status:', error);
        alert("Error changing status");
    }
}

async function deleteTask(taskId) {
    if (!confirm("Delete this task?")) return;

    try {
        const res = await fetch('?handler=DeleteTask&id=' + taskId, {
            method: "POST",
            headers: {
                "RequestVerificationToken":
                    document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });

        if (res.ok) {
            location.reload();
        } else {
            alert("Delete task failed");
        }
    } catch (error) {
        console.error('Error deleting task:', error);
        alert("Error deleting task");
    }
}

// =========================
// WIKI MANAGEMENT
// =========================
function initializeWikiManagement() {
    // Các hàm wiki sẽ được gọi từ onclick trong HTML
}

function openCreateWiki() {
    document.getElementById("wikiId").value = "";
    document.getElementById("wikiTitle").value = "";
    document.getElementById("wikiContent").value = "";
    document.getElementById("wikiModalTitle").innerText = "Create Wiki";

    const modalElement = document.getElementById("wikiModal");
    if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

function openWikiDetail(id) {
    fetch('?handler=WikiDetail&id=' + id)
        .then(function (r) { return r.json(); })
        .then(function (w) {
            document.getElementById("wikiId").value = w.id;
            document.getElementById("wikiTitle").value = w.title;
            document.getElementById("wikiContent").value = w.content;
            document.getElementById("wikiModalTitle").innerText = "Wiki Detail";

            const modalElement = document.getElementById("wikiModal");
            if (modalElement) {
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
            }
        })
        .catch(function (error) {
            console.error('Error loading wiki detail:', error);
            alert("Error loading wiki");
        });
}

function saveWiki() {
    const id = document.getElementById("wikiId") ? document.getElementById("wikiId").value : '';
    const projectId = document.getElementById("projectId") ? document.getElementById("projectId").value : null;
    const title = document.getElementById("wikiTitle") ? document.getElementById("wikiTitle").value : '';
    const content = document.getElementById("wikiContent") ? document.getElementById("wikiContent").value : '';

    if (!projectId) {
        alert("Project ID not found");
        return;
    }

    fetch('?handler=' + (id ? "EditWiki" : "CreateWiki"), {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "RequestVerificationToken":
                document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({
            id: id,
            projectId: projectId,
            title: title,
            content: content
        })
    })
        .then(function () { location.reload(); })
        .catch(function (error) {
            console.error('Error saving wiki:', error);
            alert("Error saving wiki");
        });
}

async function deleteWiki(id) {
    if (!confirm("Delete this wiki page?")) return;

    try {
        const res = await fetch('?handler=DeleteWiki&id=' + id, {
            method: "POST",
            headers: {
                "RequestVerificationToken":
                    document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });

        if (res.ok) {
            location.reload();
        } else {
            alert("Delete failed");
        }
    } catch (error) {
        console.error('Error deleting wiki:', error);
        alert("Error deleting wiki");
    }
}

// =========================
// TAB MANAGEMENT
// =========================
function initializeTabManagement() {
    document.querySelectorAll('#projectTabs .nav-link')
        .forEach(function (tab) {
            tab.addEventListener('click', function () {
                document.querySelectorAll('#projectTabs .nav-link')
                    .forEach(function (t) { t.classList.remove('active'); });

                document.querySelectorAll('.tab-content')
                    .forEach(function (c) { c.classList.add('d-none'); });

                this.classList.add('active');

                const target = this.dataset.tab;
                const content = document.getElementById('tab-' + target);

                if (content) {
                    content.classList.remove('d-none');
                }
            });
        });
}

// =========================
// PROJECT DELETE
// =========================
function confirmDeleteProject() {
    const input = document.getElementById("projectId");
    if (!input) {
        alert("ProjectId not found");
        return;
    }

    const projectId = input.value;

    fetch("?handler=DeleteProject", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "RequestVerificationToken":
                document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({ projectId: projectId })
    })
        .then(function (r) { return r.json(); })
        .then(function (res) {
            if (res.success) {
                window.location.href = res.redirectUrl;
            } else {
                alert("Delete failed");
            }
        })
        .catch(function (error) {
            console.error('Error deleting project:', error);
            alert("Error deleting project");
        });
}

// =========================
// TASK COMMENTS & MENTIONS
// =========================
function initializeTaskComments() {
    const taskBoard = document.getElementById("task-board");
    if (taskBoard) {
        taskBoard.addEventListener("click", function (e) {
            if (e.target.closest(".dropdown")) return;

            const header = e.target.closest(".task-header");
            if (!header) return;

            const taskId = header.dataset.taskId;
            const detail = document.getElementById('task-detail-' + taskId);
            if (!detail) return;

            const collapse = new bootstrap.Collapse(detail, { toggle: true });

            // Load comments when task detail is shown
            detail.addEventListener('shown.bs.collapse', function () {
                loadComments(taskId);
            });
        });
    }
}

// MENTION FUNCTIONALITY
function setupMentionForTask(taskId) {
    const textarea = document.querySelector('.comment-input[data-task-id="' + taskId + '"]');
    if (!textarea) return;

    textarea.addEventListener('keydown', async function (e) {
        if (e.key === '@' && !e.ctrlKey && !e.altKey && !e.metaKey) {
            e.preventDefault();

            currentTaskIdForMention = taskId;
            currentTextareaForMention = textarea;

            const cursorPos = textarea.selectionStart;
            const text = textarea.value;
            const newText = text.substring(0, cursorPos) + '@' + text.substring(cursorPos);
            textarea.value = newText;

            textarea.selectionStart = cursorPos + 1;
            textarea.selectionEnd = cursorPos + 1;

            await loadMentionMembers(taskId);
        }
    });
}

async function loadMentionMembers(taskId) {
    const projectId = document.getElementById("projectId") ? document.getElementById("projectId").value : null;
    if (!projectId) return;

    try {
        const res = await fetch('?handler=ProjectMembers&projectId=' + projectId);
        if (!res.ok) {
            console.error('Failed to load members');
            return;
        }

        const members = await res.json();
        const mentionList = document.getElementById("mention-list");
        if (!mentionList) return;

        mentionList.innerHTML = "";

        if (members.length === 0) {
            mentionList.innerHTML = '<li class="list-group-item text-muted">No members found</li>';
        } else {
            members.forEach(function (member) {
                const li = document.createElement("li");
                li.className = "list-group-item list-group-item-action";
                li.style.cursor = "pointer";

                const avatarUrl = member.avatarUrl || '/images/avatar-default.png';
                li.innerHTML = '<div class="d-flex align-items-center">' +
                    '<img src="' + avatarUrl + '" width="24" height="24" class="rounded-circle me-2">' +
                    '<div>' +
                    '<div class="fw-semibold">' + member.fullName + '</div>' +
                    '<small class="text-muted">' + (member.email || '') + '</small>' +
                    '</div>' +
                    '</div>';

                li.onclick = function () {
                    insertMention(member);
                };

                mentionList.appendChild(li);
            });
        }

        const modalElement = document.getElementById("mentionModal");
        if (modalElement) {
            const modal = new bootstrap.Modal(modalElement);
            modal.show();
        }
    } catch (error) {
        console.error('Error loading mention members:', error);
    }
}

function insertMention(member) {
    if (!currentTextareaForMention) return;

    const mentionText = '@{' + member.id + '}|' + member.fullName + ' ';
    const cursorPos = currentTextareaForMention.selectionStart;
    const text = currentTextareaForMention.value;

    const textBeforeCursor = text.substring(0, cursorPos);
    const lastAtPos = textBeforeCursor.lastIndexOf('@');

    if (lastAtPos !== -1) {
        const newText = text.substring(0, lastAtPos) + mentionText + text.substring(cursorPos);
        currentTextareaForMention.value = newText;

        const newCursorPos = lastAtPos + mentionText.length;
        currentTextareaForMention.selectionStart = newCursorPos;
        currentTextareaForMention.selectionEnd = newCursorPos;
    }

    const modalElement = document.getElementById("mentionModal");
    if (modalElement) {
        const modal = bootstrap.Modal.getInstance(modalElement);
        if (modal) modal.hide();
    }

    currentTextareaForMention.focus();
}

async function loadComments(taskId) {
    try {
        const res = await fetch('?handler=TaskComments&taskId=' + taskId);
        const comments = await res.json();

        const commentList = document.querySelector('.comments[data-task-id="' + taskId + '"] .comment-list');
        if (commentList) {
            commentList.innerHTML = renderComments(comments);
        }

        setupMentionForTask(taskId);
    } catch (error) {
        console.error('Error loading comments:', error);
    }
}

function renderComments(comments) {
    let html = '';
    comments.forEach(function (comment) {
        const avatarUrl = comment.avatarUrl || '/images/avatar-default.png';
        const createdAt = new Date(comment.createdAt).toLocaleString();
        const content = renderContentWithMentions(comment.content);
        const attachmentsHtml = renderAttachments(comment.attachments);
        html += '<div class="comment mb-2" id="comment-' + comment.id + '">' +
            '<div class="d-flex">' +
            '<img src="' + avatarUrl + '" width="32" height="32" class="rounded-circle me-2">' +
            '<div class="flex-grow-1">' +
            '<div class="d-flex justify-content-between">' +
            '<strong>' + comment.userName + '</strong>' +
            '<small class="text-muted">' + createdAt + '</small>' +
            '</div>' +
            '<div class="mt-1">' + content + '</div>' +
            attachmentsHtml +
            '<button class="btn btn-sm btn-link p-0 mt-1" onclick="setReplyTo(\'' +
            comment.taskId + '\', \'' + comment.id + '\', \'' + comment.userName + '\')">Reply</button>';


        if (comment.replies && comment.replies.length > 0) {
            comment.replies.forEach(function (reply) {
                const replyAvatarUrl = reply.avatarUrl || '/images/avatar-default.png';
                const replyCreatedAt = new Date(reply.createdAt).toLocaleString();
                const replyContent = renderContentWithMentions(reply.content);

                html += '<div class="reply mt-2 ms-4 border-start ps-3">' +
                    '<div class="d-flex">' +
                    '<img src="' + replyAvatarUrl + '" width="24" height="24" class="rounded-circle me-2">' +
                    '<div class="flex-grow-1">' +
                    '<div class="d-flex justify-content-between">' +
                    '<strong>' + reply.userName + '</strong>' +
                    '<small class="text-muted">' + replyCreatedAt + '</small>' +
                    '</div>' +
                    '<div class="mt-1">' + replyContent + '</div>' +
                    '</div>' +
                    '</div>' +
                    '</div>';
            });
        }

        html += '</div></div></div>';
    });

    return html;
}

function renderContentWithMentions(content) {
    return content.replace(/@\{([0-9a-fA-F-]{36})\}\|([^ \n]+)/g,
        '<span class="badge bg-info text-dark">@$2</span>'
    );
}

function setReplyTo(taskId, parentId, userName) {
    const replyInfo = document.querySelector('.comments[data-task-id="' + taskId + '"] .reply-info');
    const cancelBtn = document.querySelector('.comments[data-task-id="' + taskId + '"] .cancel-reply');
    const commentInput = document.querySelector('.comments[data-task-id="' + taskId + '"] .comment-input');

    if (replyInfo) {
        replyInfo.innerHTML = 'Replying to <strong>' + userName + '</strong>';
        replyInfo.classList.remove('d-none');
    }

    if (cancelBtn) {
        cancelBtn.classList.remove('d-none');
        cancelBtn.dataset.taskId = taskId;
        cancelBtn.dataset.parentId = parentId;
    }

    if (commentInput) {
        commentInput.dataset.parentId = parentId;
        commentInput.focus();
    }
}

function cancelReply(taskId) {
    const replyInfo = document.querySelector('.comments[data-task-id="' + taskId + '"] .reply-info');
    const cancelBtn = document.querySelector('.comments[data-task-id="' + taskId + '"] .cancel-reply');
    const commentInput = document.querySelector('.comments[data-task-id="' + taskId + '"] .comment-input');

    if (replyInfo) {
        replyInfo.innerHTML = '';
        replyInfo.classList.add('d-none');
    }

    if (cancelBtn) {
        cancelBtn.classList.add('d-none');
        delete cancelBtn.dataset.parentId;
    }

    if (commentInput) {
        delete commentInput.dataset.parentId;
        commentInput.value = '';
    }
}

async function postComment(taskId) {
    const container = document.querySelector(
        '.comments[data-task-id="' + taskId + '"]'
    );

    if (!container) return;

    const commentInput = container.querySelector('.comment-input');
    const fileInput = container.querySelector('.comment-attachments');

    const content = commentInput.value.trim();
    const parentId = commentInput.dataset.parentId || null;

    if (!content && fileInput.files.length === 0) return;

    const formData = new FormData();
    formData.append("TaskId", taskId);
    formData.append("Content", content);

    if (parentId) {
        formData.append("ParentId", parentId);
    }

    // 🔥 FILES
    for (const file of fileInput.files) {
        formData.append("Attachments", file);
    }

    try {
        const res = await fetch("?handler=CreateComment", {
            method: "POST",
            headers: {
                "RequestVerificationToken":
                    document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: formData
        });

        if (res.ok) {
            commentInput.value = '';
            fileInput.value = '';
            cancelReply(taskId);
            await loadComments(taskId);
        } else {
            alert("Failed to post comment");
        }
    } catch (error) {
        console.error("Error posting comment:", error);
        alert("Error posting comment");
    }
}

// =========================
// MAIN INITIALIZATION
// =========================
document.addEventListener("DOMContentLoaded", function () {
    // Initialize all functionalities
    initializeMemberManagement();
    initializeTaskManagement();
    initializeWikiManagement();
    initializeTabManagement();
    initializeTaskComments();

    console.log("Project details initialized");
});

function renderAttachments(attachments) {
    if (!attachments || attachments.length === 0) return '';

    let html = '<div class="mt-2 attachments">';
    attachments.forEach(function (a) {
        const icon = a.contentType.startsWith('image/')
            ? '🖼️'
            : '📎';

        html += `
            <div class="small">
                ${icon}
                <a href="${a.filePath}" target="_blank">
                    ${a.fileName}
                </a>
            </div>
        `;
    });
    html += '</div>';

    return html;
}
