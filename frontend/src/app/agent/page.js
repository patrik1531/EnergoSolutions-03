"use client";

import ReactMarkdown from "react-markdown";
import { useEffect, useRef, useState } from "react";
import arrow from "../../../public/next.png";
import Image from "next/image";
import Head from "next/head";

export default function AgentPage() {
    const [messages, setMessages] = useState([]);
    const [input, setInput] = useState("");
    const [isSending, setIsSending] = useState(false);
    const [isInitializing, setIsInitializing] = useState(true);
    const [showThinking, setShowThinking] = useState(false); // loader bublina

    const sessionIdRef = useRef("");
    const chatRef = useRef(null);
    const inputRef = useRef(null);
    const thinkingTimeoutRef = useRef(null);

    // auto scroll na spodok pri novej správe / keď sa objaví loader
    useEffect(() => {
        if (chatRef.current) {
            chatRef.current.scrollTop = chatRef.current.scrollHeight;
        }
    }, [messages, showThinking]);

    function addMessage(text, sender) {
        if (!text) return;
        setMessages((prev) => [
            ...prev,
            { id: `${Date.now()}-${Math.random()}`, text, sender }
        ]);
    }

    function clearThinkingTimeout() {
        if (thinkingTimeoutRef.current) {
            clearTimeout(thinkingTimeoutRef.current);
            thinkingTimeoutRef.current = null;
        }
    }

    // cleanup pri unmount
    useEffect(() => {
        return () => {
            clearThinkingTimeout();
        };
    }, []);

    // 1) Načítanie úvodnej správy zo /api/Chat/start
    useEffect(() => {
        const startChat = async () => {
            try {
                setIsInitializing(true);

                const res = await fetch("http://10.10.131.1:5108/api/Chat/start", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    }
                });

                if (!res.ok) {
                    console.error("Start chat failed with status", res.status);
                    addMessage(
                        "Ospravedlňujem sa, nepodarilo sa načítať úvodnú správu. Skúste to prosím neskôr.",
                        "agent"
                    );
                    return;
                }

                const data = await res.json();

                if (data.sessionId) {
                    sessionIdRef.current = data.sessionId;
                } else {
                    console.error("sessionId chýba v odpovedi /api/Chat/start");
                }

                if (data.message) {
                    setMessages([
                        {
                            id: "init",
                            text: data.message,
                            sender: "agent"
                        }
                    ]);
                }
            } catch (err) {
                console.error("Error starting chat:", err);
                addMessage(
                    "Ospravedlňujem sa, nastala chyba pri načítaní úvodnej správy.",
                    "agent"
                );
            } finally {
                setIsInitializing(false);
                inputRef.current?.focus();
            }
        };

        startChat();
    }, []);

    // 2) Posielanie správy do /api/Chat/message
    async function sendMessage() {
        const trimmed = input.trim();
        if (!trimmed || isSending) return;

        if (!sessionIdRef.current) {
            addMessage(
                "Ospravedlňujem sa, spojenie so serverom ešte nie je pripravené. Skúste to prosím o chvíľu.",
                "agent"
            );
            return;
        }

        setInput("");
        addMessage(trimmed, "user");
        setIsSending(true);

        // reset loadera
        clearThinkingTimeout();
        setShowThinking(false);

        // ak odpoveď trvá dlhšie ako 1.5s → zobraz „premýšľam…“
        thinkingTimeoutRef.current = setTimeout(() => {
            setShowThinking(true);
        }, 1500);

        try {
            const res = await fetch("http://10.10.131.1:5108/api/Chat/message", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    sessionId: sessionIdRef.current,
                    message: trimmed
                })
            });

            if (!res.ok) {
                console.error("Chat request failed with status", res.status);
                addMessage(
                    "Ospravedlňujem sa, nastala chyba pri komunikácii so serverom.",
                    "agent"
                );
                return;
            }

            const data = await res.json();

            const replyText =
                typeof data.message === "string"
                    ? data.message
                    : "Dostal som odpoveď, ale neviem ju zobraziť.";

            addMessage(replyText, "agent");
        } catch (err) {
            console.error("Error sending message:", err);
            addMessage(
                "Ospravedlňujem sa, nastala chyba pri komunikácii so serverom.",
                "agent"
            );
        } finally {
            setIsSending(false);
            clearThinkingTimeout();
            setShowThinking(false);
            inputRef.current?.focus();
        }
    }

    function handleKeyDown(e) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    }

    return (
        <>
            <Head>
                <title>EnergoAI – Chat</title>
            </Head>
            <main className="w-full bg-[#111111] flex flex-col justify-center items-center h-screen">
                <section className="2xl:max-w-7xl xl:max-w-5xl lg:max-w-3xl md:max-w-xl sm:max-w-lg w-full px-4 h-screen flex flex-col py-4 lg:py-9 xl:py-12 gap-6">
                    <h1 className="text-2xl font-bold px-4 pb-9">🌱 EnergoSolutions</h1>

                    {/* Chat window */}
                    <div
                        ref={chatRef}
                        className="rounded-[2rem] h-full p-4 lg:p-8 bg-[#111111] overflow-y-auto flex flex-col gap-4"
                    >
                        {isInitializing && messages.length === 0 && (
                            <div className="text-sm text-gray-400">
                                Načítavam energetického poradcu...
                            </div>
                        )}

                        {messages.map((msg) => (
                            <div
                                key={msg.id}
                                className={`max-w-[80%] w-fit px-5 py-3 rounded-3xl text-sm leading-relaxed break-words whitespace-pre-line
            ${
                                    msg.sender === "user"
                                        ? "bg-[#222222] text-gray-50 self-end text-right"
                                        : "bg-[#E20074] text-gray-50 self-start"
                                }`}
                            >
                                <ReactMarkdown skipHtml={true}>
                                    {msg.text}
                                </ReactMarkdown>
                            </div>
                        ))}


                        {showThinking && !isInitializing && (
                            <div className="max-w-[60%] w-fit px-5 py-3 rounded-3xl text-sm bg-[#E20074]/70 text-gray-50 self-start flex items-center gap-3">
                                <span>Poradca premýšľa...</span>
                                <span className="flex gap-1">
                                    <span className="w-2 h-2 rounded-full bg-white/80 animate-bounce [animation-delay:-0.2s]" />
                                    <span className="w-2 h-2 rounded-full bg-white/60 animate-bounce [animation-delay:0s]" />
                                    <span className="w-2 h-2 rounded-full bg-white/40 animate-bounce [animation-delay:0.2s]" />
                                </span>
                            </div>
                        )}
                    </div>

                    {/* Input */}
                    <div className="flex gap-4 items-end">
                        <textarea
                            ref={inputRef}
                            placeholder={
                                isInitializing
                                    ? "Čakám na pripojenie k poradcovi..."
                                    : "Napíšte správu..."
                            }
                            className="flex-1 rounded-3xl px-6 py-3 text-sm bg-[#222222] text-white resize-none leading-relaxed
                                       focus:outline-none focus:ring-0 border-none"
                            rows={1}
                            value={input}
                            onChange={(e) => {
                                setInput(e.target.value);
                                e.target.style.height = "auto";
                                e.target.style.height = `${e.target.scrollHeight}px`;
                            }}
                            onKeyDown={handleKeyDown}
                            disabled={isInitializing || !sessionIdRef.current}
                        />

                        <button
                            type="button"
                            onMouseDown={(e) => e.preventDefault()} // nech neberie focus z textarea
                            onClick={sendMessage}
                            disabled={isSending || isInitializing || !sessionIdRef.current}
                            className="h-[48px] w-[48px] rounded-full bg-gray-50 text-[#222222] text-sm font-medium
                                hover:font-bold disabled:opacity-60 disabled:cursor-not-allowed
                                flex items-center justify-center whitespace-nowrap"
                        >
                            <Image src={arrow} alt="sipka" width={24} height={24} />
                        </button>
                    </div>
                </section>
            </main>
        </>
    );
}
