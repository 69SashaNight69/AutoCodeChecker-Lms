/* eslint-disable jsx-a11y/alt-text */
import React, { useState, useEffect } from 'react';
import Editor from "@monaco-editor/react";
import ReactMarkdown from 'react-markdown';
import { fetchTaskById, submitCodeToApi } from '../api';

const StudentView = ({ tasks }) => {
    const [currentTask, setCurrentTask] = useState(null);
    const [code, setCode] = useState("");
    const [customTests, setCustomTests] = useState([{ inputs: "" }]);
    const [result, setResult] = useState(null);
    const [loading, setLoading] = useState(false);

    const loadTask = async (id) => {
        setLoading(true);
        const data = await fetchTaskById(id);
        setCurrentTask(data);
        setCode(data.InitialCode);
        setResult(null);
        setLoading(false);
    };

    const submitCode = async (isSubmitAll) => {
        const validTests = customTests.filter(t => t.inputs.trim() !== "");
        if (!isSubmitAll && validTests.length === 0) {
            alert("Введіть хоча б один тест!"); return;
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
        } catch (e) { alert("Помилка!"); }
        setLoading(false);
    };

    return (
        <div style={{ display: "flex", flex: 1, overflow: "hidden" }}>
            {/* Sidebar */}
            <div style={sidebarStyle}>
                <h3>📋 Завдання</h3>
                {tasks.map(t => (
                    <div key={t.Id} onClick={() => loadTask(t.Id)} style={{ ...taskItemStyle, background: currentTask?.Id === t.Id ? "#37373d" : "transparent" }}>
                        {t.Title}
                    </div>
                ))}
            </div>

            {/* Main Area */}
            <div style={{ flex: 1, display: "flex", flexDirection: "column", padding: "20px", overflowY: "auto" }}>
                {currentTask ? (
                    <div style={{ display: "flex", gap: "20px", height: "100%" }}>
                        {/* ЛІВА ЧАСТИНА: ОПИС ЗАДАЧІ ТА ФОТО */}
                        <div style={{ flex: 1, overflowY: "auto", paddingRight: "10px" }}>
                            <h2 style={{ marginTop: 0 }}>{currentTask.Title || currentTask.title}</h2>
                            <div style={descStyle}>
                                <ReactMarkdown
                                    components={{
                                        // Ця частина автоматично стилізує всі картинки з Markdown
                                        img: ({ node, ...props }) => (
                                            <img
                                                {...props}
                                                style={{ maxWidth: "100%", height: "auto", borderRadius: "8px", marginTop: "15px", display: "block", boxShadow: "0 4px 10px rgba(0,0,0,0.3)" }}
                                                onError={(e) => { e.target.src = "https://via.placeholder.com/400x200?text=Image+Load+Error"; }}
                                            />
                                        )
                                    }}
                                >
                                    {currentTask.Description || currentTask.description}
                                </ReactMarkdown>
                            </div>
                        </div>

                        {/* ПРАВА ЧАСТИНА: РЕДАКТОР ТА ТЕСТИ */}
                        <div style={editorAreaStyle}>
                            <div style={editorContainer}>
                                <Editor height="100%" defaultLanguage="csharp" theme="vs-dark" value={code} onChange={setCode} options={{ fontSize: 13, minimap: { enabled: false } }} />
                            </div>

                            <div style={playgroundStyle}>
                                <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "8px" }}>
                                    <h5 style={{ margin: 0, color: "#aaa" }}>🧪 Playground:</h5>
                                    <button style={{ background: "none", border: "1px solid #555", color: "#ccc", cursor: "pointer", fontSize: "11px" }} onClick={() => setCustomTests([...customTests, { inputs: "" }])}>+ Add</button>
                                </div>
                                {/* підказка для тесту ввода */}
                                {(currentTask.TestCases || currentTask.testCases)?.length > 0 && (
                                    <div style={{ fontSize: "11px", color: "#8ce08c", marginBottom: "10px", fontStyle: "italic" }}>
                                        💡 Приклад формату: <b>{(currentTask.TestCases || currentTask.testCases)[0].Inputs?.join(" | ") || (currentTask.TestCases || currentTask.testCases)[0].inputs?.join(" | ")}</b>
                                    </div>
                                )}
                                <div style={{ maxHeight: "80px", overflowY: "auto" }}>
                                    {customTests.map((t, i) => (
                                        <div key={i} style={{ display: "flex", gap: "5px", marginTop: "5px" }}>
                                            <input value={t.inputs} style={inputStyle} onChange={e => {
                                                const n = [...customTests]; n[i].inputs = e.target.value; setCustomTests(n);
                                            }} placeholder="Input (e.g. 5|6)" />
                                            <button style={{ background: "#622", color: "white", border: "none", padding: "0 10px", cursor: "pointer" }} onClick={() => setCustomTests(customTests.filter((_, idx) => idx !== i))}>✕</button>
                                        </div>
                                    ))}
                                </div>
                                <div style={{ display: "flex", gap: "5px", marginTop: "10px" }}>
                                    <button onClick={() => submitCode(false)} style={{ flex: 1, padding: "8px", cursor: "pointer", background: "#444", color: "white", border: "none" }}>Run</button>
                                    <button onClick={() => submitCode(true)} style={{ flex: 2, padding: "8px", cursor: "pointer", background: "#007acc", color: "white", border: "none" }}>Submit</button>
                                </div>
                            </div>

                            {/* Results */}
                            {result && (
                                <div style={resultStyle}>
                                    <div style={{ fontSize: "12px", marginBottom: "5px", display: "flex", justifyContent: "space-between" }}>
                                        <b style={{ color: result.isRunOnly ? "#4db8ff" : (result.IsSuccess ? "#8ce08c" : "#e08c8c") }}>
                                            {result.isRunOnly ? "Output (Console):" : `Score: ${result.Score}%`}
                                        </b>
                                        <span onClick={() => setResult(null)} style={{ cursor: "pointer", color: "#888" }}>✕ close</span>
                                    </div>

                                    <div style={{ overflowY: "auto", maxHeight: "150px", fontSize: "12px", fontFamily: "monospace" }}>
                                        {result.TestResults.map((tr, i) => (
                                            <div key={i} style={{ marginBottom: "5px", borderBottom: "1px solid #333", paddingBottom: "4px" }}>
                                                <span style={{ color: result.isRunOnly ? "#4db8ff" : (tr.IsSuccess ? "#8ce08c" : "#e08c8c") }}>
                                                    {result.isRunOnly ? "▶" : (tr.IsSuccess ? "✓" : "✗")}
                                                </span>
                                                <span style={{ marginLeft: "8px" }}>
                                                    In:({tr.Input}) →
                                                </span>

                                                <b style={{ color: tr.Actual?.includes("Time Limit") ? "#e08c8c" : "#fff", marginLeft: "5px" }}>
                                                    {tr.Actual}
                                                </b>

                                                <span style={{ color: "#888", marginLeft: "15px", fontStyle: "italic" }}>
                                                    ⏱ {(tr.ExecutionTimeMs || tr.executionTimeMs) < 1
                                                        ? "< 1"
                                                        : (tr.ExecutionTimeMs || tr.executionTimeMs)} ms
                                                </span>

                                                {!result.isRunOnly && !tr.IsSuccess && tr.Expected !== "N/A" && (
                                                    <div style={{ color: "#888", marginLeft: "20px", marginTop: "2px" }}>
                                                        [Expected: {tr.Expected}]
                                                    </div>
                                                )}
                                            </div>
                                        ))}
                                    </div>
                                    {result.AiFeedback && <div style={{ color: "#4db8ff", fontSize: "11px", marginTop: "8px" }}>{result.AiFeedback}</div>}
                                </div>
                            )}
                        </div>
                    </div>
                ) : <h3>Виберіть задачу зліва</h3>}
            </div>
        </div>
    );
};

// Короткі стилі для StudentView
const sidebarStyle = { width: "250px", background: "#252526", padding: "15px", borderRight: "1px solid #444" };
const taskItemStyle = { padding: "10px", cursor: "pointer", borderRadius: "5px", marginBottom: "5px" };
const descStyle = { background: "#2d2d2d", padding: "15px", borderRadius: "8px" };
const editorAreaStyle = { flex: 1.5, display: "flex", flexDirection: "column", gap: "10px" };
const editorContainer = { flex: 1, border: "1px solid #444", borderRadius: "8px", overflow: "hidden" };
const playgroundStyle = { background: "#252526", padding: "10px", borderRadius: "8px", border: "1px solid #444" };
const inputStyle = { flex: 1, background: "#333", color: "white", border: "1px solid #555", padding: "5px" };
const resultStyle = { padding: "10px", background: "#1a1a1a", border: "1px solid #444", borderRadius: "8px", maxHeight: "120px" };

export default StudentView;