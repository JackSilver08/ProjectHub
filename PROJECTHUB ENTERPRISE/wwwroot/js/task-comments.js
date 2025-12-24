// TASK COMMENTS & MENTIONS - UNIFIED VERSION
// ===========================================

let replyingTo = null;
let currentTaskId = null;
let mentionData = {
    members: [],
    isShowing: false,
    searchTerm: '',
    filteredMembers: []
};

// =========================
// INITIALIZE COMMENTS
// =========================
document.addEventListener("DOMContentLoaded", function () {
    const taskBoard = document.getElementById("task-board");
    if (taskBoard) {
        taskBoard.addEventListener("click", function (e) {
            if (e.target.closest(".dropdown")) return;

            const header = e.target.closest(".task-header");
            if (!header) return;

            const taskId = header.dataset.taskId;
            const detail = document.getElementById('task-detail-' + taskId);
            if (!detail) return;

            // Toggle collapse
            const collapse = new bootstrap.Collapse(detail, { toggle: true });

            // Load comments when shown
            detail.addEventListener('shown.bs.collapse', function () {
                loadComments(taskId);
                setupCommentInput(taskId);
            });
        });
    }
});

// =========================
// LOAD COMMENTS
// =========================
async function loadComments(taskId) {
    try {
        currentTaskId = taskId;

        const res = await fetch(`?handler=TaskComments&taskId=${taskId}`);
        if (!res.ok) {
            console.error('Failed to load comments');
            return;
        }

        const comments = await res.json();
        const container = document
            .querySelector(`.comments[data-task-id="${taskId}"] .comment-list`);

        if (container) {
            container.innerHTML = "";
            comments.forEach(c => renderComment(c, container, 0));
        }

        // Load members for mention
        await loadProjectMembers(taskId);
    } catch (error) {
        console.error('Error loading comments:', error);
    }
}

// =========================
// LOAD PROJECT MEMBERS
// =========================
async function loadProjectMembers(taskId) {
    try {
        const projectId = document.getElementById("projectId") ? document.getElementById("projectId").value : null;
        if (!projectId) return;

        const res = await fetch(`?handler=ProjectMembers&projectId=${projectId}`);
        if (!res.ok) return;

        const members = await res.json();
        mentionData.members = members;
        console.log('Loaded members:', members.length);
    } catch (error) {
        console.error('Error loading members:', error);
    }
}

// =========================
// RENDER COMMENT WITH MENTIONS
// =========================
function renderComment(c, container, level) {
    const div = document.createElement("div");
    div.className = "comment-item mb-3";
    div.style.marginLeft = level * 20 + "px";

    // Format mentions in content
    const formattedContent = formatMentionsInContent(c.content);

    div.innerHTML = `
        <div class="d-flex gap-2">
            <img src="${c.avatarUrl || '/images/avatar-default.png'}"
                 class="rounded-circle" width="32" height="32"/>
            <div class="flex-grow-1">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <strong class="me-2">${c.userName}</strong>
                        <small class="text-muted">
                            ${formatDate(c.createdAt)}
                        </small>
                    </div>
                    <button class="btn btn-sm btn-link p-0" 
                            onclick="replyTo('${c.id}','${c.userName}','${c.taskId}')">
                        Reply
                    </button>
                </div>
                <div class="mt-1">${formattedContent}</div>
            </div>
        </div>
    `;

    container.appendChild(div);

    // Render replies
    if (c.replies && c.replies.length > 0) {
        c.replies.forEach(r => renderComment(r, container, level + 1));
    }
}

// =========================
// FORMAT MENTIONS IN CONTENT
// =========================
function formatMentionsInContent(content) {
    // Parse mentions in format @{id}|name
    let formatted = content;

    // Replace @{id}|name with styled mention
    formatted = formatted.replace(
        /@\{([0-9a-fA-F-]{36})\}\|([^ \n<]+)/g,
        '<span class="mention-tag badge bg-primary bg-opacity-10 text-primary border border-primary border-opacity-25 px-2 py-1 me-1">@$2</span>'
    );

    // Also handle simple @ mentions
    formatted = formatted.replace(
        /@(\w[\w\sÀ-ỹ]*)/g,
        '<span class="mention-tag badge bg-info bg-opacity-10 text-info border border-info border-opacity-25 px-2 py-1 me-1">@$1</span>'
    );

    return formatted;
}

// =========================
// SETUP COMMENT INPUT WITH MENTION
// =========================
function setupCommentInput(taskId) {
    const textarea = document.querySelector(`.comment-input[data-task-id="${taskId}"]`);
    if (!textarea) return;

    // Clone to remove old event listeners
    const newTextarea = textarea.cloneNode(true);
    textarea.parentNode.replaceChild(newTextarea, textarea);

    // Setup mention detection on input
    newTextarea.addEventListener('input', function (e) {
        detectMention(this, taskId);
    });

    // Setup mention detection on keydown for @
    newTextarea.addEventListener('keydown', function (e) {
        if (e.key === '@') {
            e.preventDefault();
            showMentionDropdown(this, taskId);
        }
    });

    // Setup post button
    const postBtn = newTextarea.closest('.comments').querySelector('.btn-primary');
    if (postBtn) {
        postBtn.onclick = function () {
            postComment(taskId);
        };
    }
}

// =========================
// DETECT MENTION IN INPUT
// =========================
function detectMention(textarea, taskId) {
    const cursorPos = textarea.selectionStart;
    const textBeforeCursor = textarea.value.substring(0, cursorPos);

    // Find the last @ symbol before cursor
    const lastAtPos = textBeforeCursor.lastIndexOf('@');

    if (lastAtPos !== -1) {
        // Check if this is a mention trigger (not part of another word)
        const charBeforeAt = lastAtPos > 0 ? textBeforeCursor[lastAtPos - 1] : ' ';

        if (/\s/.test(charBeforeAt) || lastAtPos === 0) {
            // Get text after @
            const textAfterAt = textBeforeCursor.substring(lastAtPos + 1);
            const words = textAfterAt.split(/\s+/);
            const searchTerm = words[0];

            if (searchTerm.length > 0) {
                showMentionDropdown(textarea, taskId, searchTerm);
            }
        }
    }
}

// =========================
// SHOW MENTION DROPDOWN
// =========================
function showMentionDropdown(textarea, taskId, searchTerm = '') {
    if (mentionData.members.length === 0) return;

    // Filter members based on search term
    mentionData.filteredMembers = mentionData.members.filter(member => {
        if (!searchTerm) return true;

        const nameMatch = member.fullName.toLowerCase().includes(searchTerm.toLowerCase());
        const emailMatch = member.email && member.email.toLowerCase().includes(searchTerm.toLowerCase());
        return nameMatch || emailMatch;
    });

    if (mentionData.filteredMembers.length === 0) return;

    // Create or update dropdown
    let dropdown = document.getElementById('mention-dropdown');
    if (!dropdown) {
        dropdown = document.createElement('div');
        dropdown.id = 'mention-dropdown';
        dropdown.className = 'mention-dropdown position-absolute bg-white border rounded shadow-sm';
        dropdown.style.zIndex = '1050';
        document.body.appendChild(dropdown);
    }

    // Build dropdown content
    dropdown.innerHTML = '';
    mentionData.filteredMembers.forEach((member, index) => {
        const item = document.createElement('div');
        item.className = 'mention-item p-2 border-bottom cursor-pointer';
        item.innerHTML = `
            <div class="d-flex align-items-center">
                <img src="${member.avatarUrl || '/images/avatar-default.png'}" 
                     width="24" height="24" class="rounded-circle me-2">
                <div>
                    <div class="fw-semibold">${member.fullName}</div>
                    <small class="text-muted">${member.email || ''}</small>
                </div>
            </div>
        `;

        item.addEventListener('click', () => {
            insertMention(textarea, member, searchTerm);
            dropdown.remove();
        });

        dropdown.appendChild(item);
    });

    // Position dropdown near cursor in textarea
    positionDropdownNearTextarea(dropdown, textarea);
    mentionData.isShowing = true;
}

// =========================
// POSITION DROPDOWN
// =========================
function positionDropdownNearTextarea(dropdown, textarea) {
    const rect = textarea.getBoundingClientRect();
    const cursorPos = textarea.selectionStart;

    // Create a temporary span to measure cursor position
    const tempSpan = document.createElement('span');
    tempSpan.style.visibility = 'hidden';
    tempSpan.style.whiteSpace = 'pre-wrap';
    tempSpan.style.font = window.getComputedStyle(textarea).font;
    tempSpan.textContent = textarea.value.substring(0, cursorPos);

    document.body.appendChild(tempSpan);
    const spanWidth = tempSpan.offsetWidth;
    const spanHeight = tempSpan.offsetHeight;
    document.body.removeChild(tempSpan);

    // Calculate position
    const lineHeight = parseInt(window.getComputedStyle(textarea).lineHeight);
    const lines = Math.floor(spanHeight / lineHeight);

    dropdown.style.top = (rect.top + (lines * lineHeight) + scrollY + 5) + 'px';
    dropdown.style.left = (rect.left + spanWidth + scrollX) + 'px';
    dropdown.style.maxWidth = '300px';
    dropdown.style.maxHeight = '200px';
    dropdown.style.overflowY = 'auto';
}

// =========================
// INSERT MENTION
// =========================
function insertMention(textarea, member, searchTerm = '') {
    const cursorPos = textarea.selectionStart;
    const text = textarea.value;

    // Find the @ position to replace
    const textBeforeCursor = text.substring(0, cursorPos);
    const lastAtPos = textBeforeCursor.lastIndexOf('@');

    if (lastAtPos !== -1) {
        // Calculate how much text to replace (including search term if any)
        let replaceLength = 1; // @
        if (searchTerm) {
            const afterAt = textBeforeCursor.substring(lastAtPos + 1);
            const spacePos = afterAt.indexOf(' ');
            replaceLength += spacePos === -1 ? afterAt.length : spacePos;
        }

        // Create mention text
        const mentionText = `@{${member.id}}|${member.fullName} `;

        // Replace from @ to current cursor position
        const beforeMention = text.substring(0, lastAtPos);
        const afterMention = text.substring(lastAtPos + replaceLength);
        textarea.value = beforeMention + mentionText + afterMention;

        // Set cursor position after mention
        const newCursorPos = lastAtPos + mentionText.length;
        textarea.selectionStart = newCursorPos;
        textarea.selectionEnd = newCursorPos;

        // Hide dropdown
        const dropdown = document.getElementById('mention-dropdown');
        if (dropdown) dropdown.remove();
        mentionData.isShowing = false;

        textarea.focus();
    }
}

// =========================
// REPLY FUNCTIONALITY
// =========================
function replyTo(commentId, userName, taskId) {
    replyingTo = commentId;
    currentTaskId = taskId;

    const box = document.querySelector(`.comments[data-task-id="${taskId}"]`);
    if (!box) return;

    box.querySelector(".reply-info").innerHTML =
        `<strong>Replying to ${userName}</strong>`;
    box.querySelector(".reply-info").classList.remove("d-none");
    box.querySelector(".cancel-reply").classList.remove("d-none");

    const textarea = box.querySelector(".comment-input");
    textarea.focus();
}

function cancelReply(taskId) {
    replyingTo = null;

    const box = document.querySelector(`.comments[data-task-id="${taskId}"]`);
    if (!box) return;

    box.querySelector(".reply-info").classList.add("d-none");
    box.querySelector(".cancel-reply").classList.add("d-none");
}

// =========================
// POST COMMENT
// =========================
async function postComment(taskId) {
    const box = document.querySelector(`.comments[data-task-id="${taskId}"]`);
    if (!box) return;

    const textarea = box.querySelector(".comment-input");
    const content = textarea.value.trim();
    if (!content) return;

    try {
        const res = await fetch("?handler=CreateComment", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken":
                    document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                taskId: taskId,
                content: content,
                parentId: replyingTo
            })
        });

        if (res.ok) {
            textarea.value = "";
            cancelReply(taskId);
            await loadComments(taskId);
        } else {
            alert("Failed to post comment");
        }
    } catch (error) {
        console.error('Error posting comment:', error);
        alert("Error posting comment");
    }
}

// =========================
// UTILITY FUNCTIONS
// =========================
function formatDate(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return date.toLocaleDateString();
}

// =========================
// CLOSE DROPDOWN ON CLICK OUTSIDE
// =========================
document.addEventListener('click', function (e) {
    if (mentionData.isShowing) {
        const dropdown = document.getElementById('mention-dropdown');
        if (dropdown && !dropdown.contains(e.target)) {
            dropdown.remove();
            mentionData.isShowing = false;
        }
    }
});

// =========================
// EXPORT FUNCTIONS FOR HTML
// =========================
// Make functions available globally for onclick handlers
window.loadComments = loadComments;
window.replyTo = replyTo;
window.cancelReply = cancelReply;
window.postComment = postComment;