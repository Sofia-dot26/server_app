const notification = document.getElementById('notification');
const roles = {
    admin: '�������������',
    dir: '���������',
    acc: '�������',
};
const roles_rp = {
    admin: '��������������',
    dir: '���������� �������������',
    acc: '�������',
};

//����������� 
function showNotification(message) {
    notification.innerText = message.trim();
    if (notification.innerText !== "") {
        notification.style.display = 'block';
        notification.onclick = () => notification.style.display = 'none';
        setTimeout(() => notification.style.display = 'none', 5000);
    }
}

//������ � API
const api = new ApiService(window.location.origin);

function showAllowedViews(allowedViews) {
    document.getElementById('control-panel').innerHTML = `������ ���������� ${roles_rp[user_role] || user_role}`;
    document.getElementById('greetings').innerHTML = `������������, ${user.login}!`;

    let element = document.getElementById('dashboard-links'); // ��������� ��� ������
    element.innerHTML = ''; // �������
    for (let viewId of allowedViews) { // � ���������� ����������� ������
        let link = document.createElement('div');
        link.attributes['data-id'] = viewId;
        link.innerHTML = `<button class="dashboard-button" onclick="view('${viewId}View')">${window.views[viewId].title_main}</button>`;
        element.appendChild(link);
    }

    // ��������� DashboardView
    view("DashboardView");
}
function login(login, password) {
    api.login(login, password)
        .then(response => {
            try {
                if (response.success) {
                    // ��������� ������ ������������ � ������
                    window.user = response.user;
                    window.session = response.session;
                    window.session_id = response.session_id;
                    window.user_role = response.user_role;
                    window.allowed_controllers = response.allowed_controllers;
                    window.allowed_views = response.allowed_views;

                    // ��������� session_id � localStorage
                    localStorage.setItem("session_id", response.session_id);

                    // ��������� ��������� ������ �� ��������
                    showAllowedViews(response.allowed_views || []);
                } else {
                    // � ������ ������ ����������� ���������� ���������
                    showNotification(response.message || "������ �����������");
                    view("LoginView");
                }
            } catch (err) {
                // ��������� ������
                showNotification(`������ �������: ${err.message}`);
                view("LoginView");
            }
        });
}
function buttonLoginClick() {
    const username = document.getElementById("lv-login").value.trim();
    const password = document.getElementById("lv-password").value.trim();

    if (!username || !password) {
        showNotification("������� ����� � ������");
        return;
    }

    login(username, password);
}

function buttonLogoutClick() {
    api
        .logout()
        .then(response => {
            view('LoginView')
        });
}


// ����������� �� ������
async function initializeApp() {
    // ��������� ������� �������
    await doInitViews();

    // �������� ����������� ������ �����
    document.getElementById("lb-enter").addEventListener("click", buttonLoginClick);
    document.getElementById("logout-button").addEventListener("click", buttonLogoutClick);

    // �������� ��������� �����������
    const session_id = localStorage.getItem('session_id');
    if (session_id) {
        api.loginState() // ���������� �� ������ ������ ���������
            .then(state => { // ��� ��������� ��������� ���� �������-����, ���� ������� ������ ������
                if (state.valid) {
                    window.user = state.user;
                    window.session = state.session;
                    window.session_id = state.session_id;
                    window.user_role = state.user_role;
                    window.allowed_controllers = state.allowed_controllers;
                    window.allowed_views = state.allowed_views;

                    showAllowedViews(state.allowed_views || []);
                    return;
                }
                view('LoginView');
            });
    } else {
        view('LoginView');
    }
}
// �������� ��������� ��������
document.addEventListener('DOMContentLoaded', initializeApp);