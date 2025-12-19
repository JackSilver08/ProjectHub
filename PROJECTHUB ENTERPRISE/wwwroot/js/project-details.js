let selectedUserId = null;

async function searchUser(email) {
    const res = await fetch(`/api/users/search?email=${email}`);
    const users = await res.json();

    const list = document.getElementById('userSearchResults');
    list.innerHTML = '';

    users.forEach(u => {
        const li = document.createElement('li');
        li.className = 'list-group-item list-group-item-action';
        li.innerText = `${u.fullName} (${u.email})`;
        li.onclick = () => selectUser(u.id);
        list.appendChild(li);
    });
}

function selectUser(userId) {
    selectedUserId = userId;
    document.getElementById('addMemberBtn').disabled = false;
}

async function addMember(projectId) {
    await fetch(`/Projects/Details?handler=AddMember`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken':
                document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({
            projectId,
            userId: selectedUserId
        })
    });

    location.reload();
}
