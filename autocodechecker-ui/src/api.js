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
    const method = task.Id ? "PUT" : "POST";
    const url = task.Id ? `${API_URL}/tasks/${task.Id}` : `${API_URL}/tasks`;
    const res = await fetch(url, {
        method: method,
        headers: getHeaders(),
        body: JSON.stringify(task)
    });
    if (!res.ok) throw res;
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

export const fetchTeacherResults = async () => {
    const res = await fetch(`${API_URL}/teacher/results`, { headers: getHeaders() });
    return res.json();
};

export const fetchMyResults = async () => {
    const res = await fetch(`${API_URL}/student/my-results`, { headers: getHeaders() });
    return res.json();
};