const API_URL = "http://localhost:5146/api";

const getHeaders = () => {
    const userData = localStorage.getItem("user");
    const headers = { "Content-Type": "application/json" };

    if (userData) {
        const parsedData = JSON.parse(userData);
        if (parsedData.Token) {
            headers["Authorization"] = `Bearer ${parsedData.Token}`;
        }
    }
    return headers;
};

export const login = async (email, password) => {
    const res = await fetch(`${API_URL}/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password })
    });
    if (!res.ok) throw new Error("Невірний логін або пароль");
    const data = await res.json();
    localStorage.setItem("user", JSON.stringify(data));
    return data;
};

export const register = async (fullName, email, password, role) => {
    const res = await fetch(`${API_URL}/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fullName, email, password, role: parseInt(role) })
    });
    if (!res.ok) {
        const errorText = await res.text();
        throw new Error(errorText || "Помилка реєстрації");
    }
    return res;
};

export const fetchTasks = async () => {
    const res = await fetch(`${API_URL}/tasks`, { headers: getHeaders() });
    if (!res.ok) throw res;
    return res.json();
};

export const fetchTaskById = async (id) => {
    const res = await fetch(`${API_URL}/tasks/${id}`, { headers: getHeaders() });
    if (!res.ok) throw res;
    return res.json();
};

export const submitCodeToApi = async (payload) => {
    const res = await fetch(`${API_URL}/assess`, {
        method: "POST",
        headers: getHeaders(),
        body: JSON.stringify(payload)
    });
    if (!res.ok) throw res;
    return res.json();
};

export const saveTaskToApi = async (task) => {
    const { Id, id, ...taskData } = task;
    const finalId = Id || id;

    const method = finalId ? "PUT" : "POST";
    const url = finalId ? `${API_URL}/tasks/${finalId}` : `${API_URL}/tasks`;

    const res = await fetch(url, {
        method: method,
        headers: getHeaders(),
        body: JSON.stringify(taskData)
    });

    if (!res.ok) {
        const errorText = await res.text();
        console.error("Server error message:", errorText);
        throw new Error(errorText);
    }
    return res;
};

export const deleteTaskFromApi = async (id) => {
    const res = await fetch(`${API_URL}/tasks/${id}`, {
        method: "DELETE",
        headers: getHeaders()
    });
    if (!res.ok) throw res;
    return res;
};

export const fetchTeacherResults = async (search = "", group = "", sortBy = "date") => {
    const params = new URLSearchParams({ search, group, sortBy }).toString();
    const res = await fetch(`${API_URL}/teacher/results?${params}`, { headers: getHeaders() });
    return res.json();
};

export const fetchMyResults = async () => {
    const res = await fetch(`${API_URL}/student/my-results`, { headers: getHeaders() });
    return res.json();
};

export const joinGroup = async (code) => {
    const res = await fetch(`${API_URL}/groups/join`, {
        method: "POST",
        headers: getHeaders(),
        body: JSON.stringify({ code })
    });
    if (!res.ok) throw new Error("Невірний код");
    return res.json();
};

export const fetchTeacherGroups = async () => {
    const res = await fetch(`${API_URL}/teacher/groups`, { headers: getHeaders() });
    return res.json();
};

export const fetchGroupStudents = async (groupId) => {
    const res = await fetch(`${API_URL}/teacher/groups/${groupId}/students`, {
        headers: getHeaders()
    });
    if (!res.ok) throw new Error("Не вдалося завантажити студентів");
    return res.json();
};

export const updateGroup = async (id, data) => {
    return fetch(`${API_URL}/groups/${id}`, {
        method: "PUT",
        headers: getHeaders(),
        body: JSON.stringify(data)
    });
};

export const deleteGroup = async (id) => {
    return fetch(`${API_URL}/groups/${id}`, { method: "DELETE", headers: getHeaders() });
};

export const removeStudentFromGroup = async (groupId, studentId) => {
    return fetch(`${API_URL}/groups/${groupId}/students/${studentId}`, {
        method: "DELETE",
        headers: getHeaders()
    });
};