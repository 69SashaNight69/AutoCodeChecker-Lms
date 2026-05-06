const API_URL = "http://localhost:5146/api";

export const fetchTasks = async () => {
    const res = await fetch(`${API_URL}/tasks`);
    return await res.json();
};

export const fetchTaskById = async (id) => {
    const res = await fetch(`${API_URL}/tasks/${id}`);
    return await res.json();
};

export const submitCodeToApi = async (payload) => {
    const response = await fetch(`${API_URL}/assess`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    });
    return await response.json();
};

export const saveTaskToApi = async (task) => {
    const method = task.Id ? "PUT" : "POST";
    const url = task.Id ? `${API_URL}/tasks/${task.Id}` : `${API_URL}/tasks`;
    return fetch(url, {
        method: method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(task)
    });
};

export const deleteTaskFromApi = async (id) => {
    return fetch(`${API_URL}/tasks/${id}`, { method: "DELETE" });
};