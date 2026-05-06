import React, { useState } from 'react';
import { login, register } from '../api';

const Auth = ({ onLogin }) => {
    const [isLogin, setIsLogin] = useState(true);
    const [formData, setFormData] = useState({ fullName: '', email: '', password: '', role: 0 });

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            if (isLogin) {
                const userData = await login(formData.email, formData.password);
                onLogin(userData);
            } else {
                await register(formData.fullName, formData.email, formData.password, formData.role);
                alert("Реєстрація успішна! Тепер увійдіть.");
                setIsLogin(true);
            }
        } catch (err) {
            alert(err.message);
        }
    };

    return (
        <div style={styles.container}>
            <form onSubmit={handleSubmit} style={styles.card}>
                <h2 style={{ color: "#4db8ff", textAlign: "center" }}>{isLogin ? "Вхід" : "Реєстрація"}</h2>
                {!isLogin && (
                    <>
                        <input style={styles.input} placeholder="Повне ім'я" onChange={e => setFormData({ ...formData, fullName: e.target.value })} required />
                        <select style={styles.input} onChange={e => setFormData({ ...formData, role: e.target.value })}>
                            <option value="0">Я студент</option>
                            <option value="1">Я викладач</option>
                        </select>
                    </>
                )}
                <input style={styles.input} type="email" placeholder="Email" onChange={e => setFormData({ ...formData, email: e.target.value })} required />
                <input style={styles.input} type="password" placeholder="Пароль" onChange={e => setFormData({ ...formData, password: e.target.value })} required />

                <button type="submit" style={styles.button}>{isLogin ? "Увійти" : "Створити акаунт"}</button>

                <p style={{ textAlign: "center", cursor: "pointer", fontSize: "14px", marginTop: "15px" }} onClick={() => setIsLogin(!isLogin)}>
                    {isLogin ? "Немає акаунту? Зареєструватися" : "Вже є акаунт? Увійти"}
                </p>
            </form>
        </div>
    );
};

const styles = {
    container: { display: "flex", justifyContent: "center", alignItems: "center", height: "100vh", background: "#121212" },
    card: { background: "#1e1e1e", padding: "40px", borderRadius: "10px", width: "350px", boxShadow: "0 10px 25px rgba(0,0,0,0.5)" },
    input: { width: "100%", padding: "12px", marginBottom: "15px", borderRadius: "5px", border: "1px solid #333", background: "#2d2d2d", color: "white", boxSizing: "border-box" },
    button: { width: "100%", padding: "12px", background: "#007acc", color: "white", border: "none", borderRadius: "5px", cursor: "pointer", fontWeight: "bold" }
};

export default Auth;