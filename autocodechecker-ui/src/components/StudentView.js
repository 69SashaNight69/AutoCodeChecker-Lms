/* eslint-disable jsx-a11y/alt-text */
import React, { useState, useEffect } from 'react';
import Editor from "@monaco-editor/react";
import ReactMarkdown from 'react-markdown';
import { fetchTaskById, submitCodeToApi, fetchMyResults, joinGroup } from '../api';

const StudentView = ({ tasks, onRefresh }) => {
    const [currentTask, setCurrentTask] = useState(null);
    const [code, setCode] = useState("");
    const [customTests, setCustomTests] = useState([{ inputs: "" }]);
    const [result, setResult] = useState(null);
    const [loading, setLoading] = useState(false);
    const [myResults, setMyResults] = useState([]);
    const [groupCode, setGroupCode] = useState("");

    const [expandedGroups, setExpandedGroups] = useState({ "Загальні завдання": true });

    useEffect(() => {
        loadMyResults();
    }, []);

    const loadMyResults = async () => {
        const data = await fetchMyResults();
        setMyResults(data);
    };

    const handleJoinGroup = async () => {
        if (!groupCode.trim()) return;
        try {
            await joinGroup(groupCode);
            alert("Ви успішно приєдналися до групи!");
            setGroupCode("");
            if (onRefresh) onRefresh();
        } catch (e) {
            alert("Помилка: невірний код");
        }
    };

    const loadTask = async (id) => {
        setLoading(true);
        const data = await fetchTaskById(id);
        setCurrentTask(data);
        setCode(data.InitialCode || data.initialCode);
        setResult(null);
        setLoading(false);
    };

    const submitCode = async (isSubmitAll) => {
        const validTests = customTests.filter(t => t.inputs.trim() !== "");
        if (!isSubmitAll && validTests.length === 0) {
            alert("Введіть хоча б один тест!");
            return;
        }

        setLoading(true);
        try {
            const payload = {
                TaskId: currentTask.Id.toString(),
                SourceCode: code,
                CustomTests: !isSubmitAll ? validTests.map(t => ({ Inputs: t.inputs.split("|") })) : null
            };

            const data = await submitCodeToApi(payload);
            data.isRunOnly = !isSubmitAll;
            setResult(data);

            if (isSubmitAll) {
                loadMyResults();
            }
        } catch (e) {
            alert("Помилка при відправці коду!");
        } finally {
            setLoading(false);
        }
    };

    const groupedTasks = tasks.reduce((acc, task) => {
        const gName = task.GroupName || "Загальні завдання";
        if (!acc[gName]) acc[gName] = [];
        acc[gName].push(task);
        return acc;
    }, {});

    return (
        <div style={{ display: "flex", flex: 1, overflow: "hidden", height: "calc(100vh - 65px)" }}>

            {/* SIDEBAR: ЗАВДАННЯ ТА ГРУПИ */}
            <div style={sidebarStyle}>
                <h3 style={{ color: "#4db8ff", marginTop: 0 }}>📋 Завдання</h3>

                <div style={{ flex: 1, overflowY: "auto", marginBottom: "15px" }}>
                    {Object.keys(groupedTasks).map(groupName => (
                        <div key={groupName} style={{ marginBottom: "10px" }}>
                            {/* Заголовок папки */}
                            <div
                                onClick={() => setExpandedGroups({ ...expandedGroups, [groupName]: !expandedGroups[groupName] })}
                                style={{ background: "#333", padding: "8px", cursor: "pointer", borderRadius: "4px", fontSize: "13px", fontWeight: "bold", marginBottom: "5px" }}
                            >
                                {expandedGroups[groupName] ? "▼" : "▶"} {groupName}
                            </div>

                            {/* Задачі в середині папки */}
                            {expandedGroups[groupName] && groupedTasks[groupName].map(t => (
                                <div
                                    key={t.Id || t.id}
                                    onClick={() => loadTask(t.Id || t.id)}
                                    style={{
                                        padding: "8px 15px", cursor: "pointer", borderRadius: "4px", fontSize: "13px", marginBottom: "2px",
                                        background: currentTask?.Id === (t.Id || t.id) ? "#37373d" : "transparent",
                                        borderLeft: currentTask?.Id === (t.Id || t.id) ? "3px solid #4db8ff" : "3px solid transparent"
                                    }}
                                >
                                    {t.Title || t.title}
                                </div>
                            ))}
                        </div>
                    ))}
                </div>

                <div style={joinGroupCard}>
                    <h4 style={{ margin: "0 0 8px 0", fontSize: "11px", color: "#888", textTransform: "uppercase" }}>Приєднатися до групи</h4>
                    <div style={{ display: "flex", gap: "5px" }}>
                        <input placeholder="Код ABC123" value={groupCode} onChange={e => setGroupCode(e.target.value)} style={smallInputStyle} />
                        <button onClick={handleJoinGroup} style={okBtn}>OK</button>
                    </div>
                </div>

                <h3 style={{ color: "#8ce08c", borderTop: "1px solid #444", paddingTop: "15px" }}>🎓 Мої успіхи</h3>
                <div style={{ height: "180px", overflowY: "auto" }}>
                    {myResults.length === 0 && <p style={{ color: "#555", fontSize: "12px" }}>Ще немає зданих робіт</p>}
                    {myResults.map((r, i) => (
                        <div key={i} style={resultCardSmall}>
                            <div style={{ fontWeight: "bold" }}>{r.Title || r.TaskTitle}</div>
                            <div style={{ color: (r.Score ?? r.score) === 100 ? "#8ce08c" : "#e08c8c" }}>Оцінка: {r.Score ?? r.score}%</div>
                        </div>
                    ))}
                </div>
            </div>

            {/* ОСНОВНА ЧАСТИНА: ОПИС ТА РЕДАКТОР */}
            <div style={{ flex: 1, display: "flex", overflow: "hidden" }}>
                {currentTask ? (
                    <div style={{ display: "flex", width: "100%", height: "100%" }}>

                        {/* ЛІВО: ОПИС */}
                        <div style={{ flex: 1, overflowY: "auto", padding: "20px", borderRight: "1px solid #333" }}>
                            <h2 style={{ marginTop: 0 }}>{currentTask.Title || currentTask.title}</h2>
                            <div style={descStyle}>
                                <ReactMarkdown components={{
                                    img: ({ node, ...props }) => <img {...props} style={{ maxWidth: "100%", borderRadius: "8px", marginTop: "15px" }} />
                                }}>
                                    {currentTask.Description || currentTask.description}
                                </ReactMarkdown>
                            </div>
                        </div>

                        {/* ПРАВО: РЕДАКТОР */}
                        <div style={{
                            flex: 1.5,
                            display: "flex",
                            flexDirection: "column",
                            background: "#1a1a1b",
                            padding: "15px",
                            gap: "10px",
                            height: "100%",
                            overflow: "hidden"
                        }}>

                            {/* Поле коду) */}
                            <div style={{
                                flex: 1,
                                border: "1px solid #444",
                                borderRadius: "8px",
                                overflow: "hidden",
                                minHeight: "150px"
                            }}>
                                <Editor
                                    height="100%"
                                    defaultLanguage="csharp"
                                    theme="vs-dark"
                                    value={code}
                                    onChange={setCode}
                                    options={{ fontSize: 14, minimap: { enabled: false }, scrollBeyondLastLine: false }}
                                />
                            </div>

                            {/* Playground */}
                            <div style={{ ...playgroundStyle, marginTop: "0px", flexShrink: 0, resize: "vertical", overflow: "auto", height: "160px", minHeight: "100px", maxHeight: "400px" }}>
                                <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "10px" }}>
                                    <h5 style={{ margin: 0, color: "#aaa" }}>🧪 Playground:</h5>
                                    <button style={addTestBtn} onClick={() => setCustomTests([...customTests, { inputs: "" }])}>+ Add Case</button>
                                </div>

                                {(currentTask.TestCases || currentTask.testCases)?.length > 0 && (
                                    <div style={{ fontSize: "11px", color: "#8ce08c", marginBottom: "10px" }}>
                                        💡 Приклад: <b>{(currentTask.TestCases || currentTask.testCases)[0].Inputs?.join(" | ")}</b>
                                    </div>
                                )}

                                <div style={{ maxHeight: "150px", overflowY: "auto" }}>
                                    {customTests.map((t, i) => (
                                        <div key={i} style={{ display: "flex", gap: "5px", marginBottom: "5px" }}>
                                            <input value={t.inputs} style={inputStyle} onChange={e => {
                                                const n = [...customTests]; n[i].inputs = e.target.value; setCustomTests(n);
                                            }} placeholder="Input (e.g. 5|6)" />
                                            <button style={removeBtn} onClick={() => setCustomTests(customTests.filter((_, idx) => idx !== i))}>✕</button>
                                        </div>
                                    ))}
                                </div>

                                <div style={{ display: "flex", gap: "10px", marginTop: "15px" }}>
                                    <button onClick={() => submitCode(false)} style={btnRun}>Run Code</button>
                                    <button onClick={() => submitCode(true)} style={btnSubmit}>Submit Solution</button>
                                </div>
                            </div>

                            {/* Консоль результатів */}
                            {result && (
                                <div style={{
                                    ...resultStyle,
                                    marginTop: "0px",
                                    flexShrink: 0,
                                    resize: "vertical",
                                    overflow: "auto",
                                    height: "140px",
                                    minHeight: "80px"
                                }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "10px" }}>
                                        <b style={{ color: result.isRunOnly ? "#4db8ff" : "#8ce08c" }}>
                                            {result.isRunOnly ? "Console Output:" : `Score: ${result.Score}%`}
                                        </b>
                                        <span onClick={() => setResult(null)} style={{ cursor: "pointer", color: "#666" }}>✕</span>
                                    </div>
                                    <div style={{ fontFamily: "monospace", fontSize: "13px" }}>
                                        {result.TestResults.map((tr, i) => (
                                            <div key={i} style={{ marginBottom: "5px", borderBottom: "1px solid #333", paddingBottom: "3px" }}>
                                                <span style={{ color: tr.IsSuccess ? "#8ce08c" : "#e08c8c" }}>{tr.IsSuccess ? "✓" : "✗"}</span>
                                                <span style={{ marginLeft: "10px" }}>In:({tr.Input}) → <b>{tr.Actual}</b></span>
                                                <span style={{ color: "#666", fontSize: "11px", marginLeft: "10px" }}>⏱ {tr.ExecutionTimeMs}ms</span>
                                            </div>
                                        ))}
                                    </div>
                                    {result.AiFeedback && <div style={aiFeedbackStyle}>{result.AiFeedback}</div>}
                                </div>
                            )}
                        </div>
                    </div>
                ) : (
                    <div style={{ display: "flex", flex: 1, alignItems: "center", justifyContent: "center", color: "#555" }}>
                        <h3>Виберіть задачу із списку ліворуч</h3>
                    </div>
                )}
            </div>
        </div>
    );
};

const sidebarStyle = { width: "280px", background: "#252526", padding: "15px", borderRight: "1px solid #444", display: "flex", flexDirection: "column" };
const groupHeaderStyle = { background: "#333", padding: "8px", cursor: "pointer", borderRadius: "4px", fontSize: "13px", fontWeight: "bold", marginBottom: "5px", color: "#ccc" };
const taskItemStyle = { padding: "8px 12px", cursor: "pointer", borderRadius: "4px", marginBottom: "3px", fontSize: "13px", transition: "0.2s" };
const joinGroupCard = { marginBottom: "20px", padding: "12px", background: "#1a1a1a", borderRadius: "8px", border: "1px solid #333" };
const smallInputStyle = { flex: 1, background: "#333", color: "white", border: "1px solid #555", padding: "6px", borderRadius: "4px", fontSize: "12px" };
const okBtn = { background: "#007acc", color: "white", border: "none", padding: "0 12px", borderRadius: "4px", cursor: "pointer" };
const descStyle = { background: "#2d2d2d", padding: "20px", borderRadius: "10px", lineHeight: "1.7", color: "#ddd" };
const playgroundStyle = { background: "#252526", padding: "15px", borderRadius: "8px", border: "1px solid #444" };
const inputStyle = { flex: 1, background: "#333", color: "white", border: "1px solid #555", padding: "10px", borderRadius: "4px" };
const addTestBtn = { background: "none", border: "1px solid #555", color: "#aaa", cursor: "pointer", fontSize: "11px", padding: "4px 10px", borderRadius: "4px" };
const removeBtn = { background: "#622", color: "white", border: "none", padding: "0 12px", borderRadius: "4px", cursor: "pointer" };
const btnRun = { flex: 1, padding: "12px", background: "#444", color: "white", border: "none", borderRadius: "5px", cursor: "pointer", fontWeight: "bold" };
const btnSubmit = { flex: 2, padding: "12px", background: "#007acc", color: "white", border: "none", borderRadius: "5px", cursor: "pointer", fontWeight: "bold" };
const resultStyle = { padding: "15px", background: "#111", border: "1px solid #333", borderRadius: "8px" };
const resultCardSmall = { padding: "8px", background: "#1a1a1a", borderRadius: "5px", marginBottom: "8px", fontSize: "12px", border: "1px solid #333" };
const aiFeedbackStyle = { marginTop: "10px", padding: "10px", borderTop: "1px solid #333", color: "#4db8ff", fontSize: "12px", fontStyle: "italic" };

export default StudentView;