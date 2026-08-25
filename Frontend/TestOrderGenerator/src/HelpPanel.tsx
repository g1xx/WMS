import { useEffect, useState } from 'react';
import axiosClient from './api/axiosClient';

// Reviewer cheat-sheet for the live demo, available from every screen. Someone opening
// the demo cold hits a login form with no credentials, and then barcode prompts with no
// idea what a valid barcode looks like — this answers both without them reading the repo.
//
// NOTE: this file is duplicated in the terminal app (warehouse-client/src/components/
// HelpPanel.tsx). The two frontends are separate Vite apps that share no build
// infrastructure, so the presentational shell is copied deliberately. Everything it
// SHOWS comes from /api/Demo/help, so there is still exactly one source of truth for the
// content — keep the copies in sync, but note that a content change belongs in
// DemoController, not here.

interface DemoLogin {
    username: string;
    password: string;
    role: string;
    description: string;
}

interface Walkthrough {
    title: string;
    steps: string[];
}

interface DemoHelp {
    logins: DemoLogin[];
    supervisorBadge: { barcode: string | null; description: string };
    availableContainers: string[];
    conveyorBarcodes: string[];
    shelfLocations: string[];
    walkthroughs: Walkthrough[];
}

const PANEL_BG = '#1e1e1e';
const CARD_BG = '#2a2a2a';
const ACCENT = '#64b5f6';

// Barcodes are the whole point of the panel and several are long GUIDs, so every one of
// them is click-to-copy rather than something to retype by hand off a screen.
function Copyable({ value }: { value: string }) {
    const [copied, setCopied] = useState(false);

    const copy = () => {
        // Older browsers and non-HTTPS origins have no clipboard API. Failing to copy is
        // not worth an error state — the value is on screen and can still be selected.
        void navigator.clipboard?.writeText(value).then(
            () => {
                setCopied(true);
                setTimeout(() => setCopied(false), 1200);
            },
            () => undefined,
        );
    };

    return (
        <button
            onClick={copy}
            title="Click to copy"
            style={{
                fontFamily: 'monospace', fontSize: '0.85rem', backgroundColor: '#333',
                color: copied ? '#4CAF50' : ACCENT, border: '1px solid #555', borderRadius: '4px',
                padding: '3px 7px', margin: '0 6px 6px 0', cursor: 'pointer',
            }}
        >
            {copied ? 'copied' : value}
        </button>
    );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
    return (
        <div style={{ marginBottom: '18px' }}>
            <h4 style={{ margin: '0 0 8px 0', color: ACCENT, fontSize: '0.95rem' }}>{title}</h4>
            {children}
        </div>
    );
}

function BarcodeList({ values, emptyNote }: { values: string[]; emptyNote: string }) {
    if (values.length === 0) {
        return <p style={{ margin: 0, color: '#888', fontSize: '0.85rem' }}>{emptyNote}</p>;
    }
    return <div style={{ display: 'flex', flexWrap: 'wrap' }}>{values.map(v => <Copyable key={v} value={v} />)}</div>;
}

export default function HelpPanel() {
    const [isOpen, setIsOpen] = useState(false);
    const [help, setHelp] = useState<DemoHelp | null>(null);

    // Probed once on mount rather than lazily on first open, because the result decides
    // whether this component renders AT ALL: when DemoSettings is off the endpoint 404s
    // and the whole panel disappears, so a real deployment shows no help button rather
    // than a button that opens an error.
    useEffect(() => {
        let cancelled = false;
        axiosClient.get<DemoHelp>('/Demo/help')
            .then(response => { if (!cancelled) setHelp(response.data); })
            .catch(() => undefined);
        return () => { cancelled = true; };
    }, []);

    // Containers get claimed by whoever else is clicking around the demo, so re-read the
    // live lists every time the panel is opened rather than showing the mount-time answer.
    const refresh = () => {
        void axiosClient.get<DemoHelp>('/Demo/help')
            .then(response => setHelp(response.data))
            .catch(() => undefined);
    };

    if (!help) return null;

    return (
        <div style={{ position: 'fixed', bottom: '16px', right: '16px', zIndex: 1000, maxWidth: 'calc(100vw - 32px)' }}>
            {isOpen && (
                <div
                    style={{
                        width: '380px', maxWidth: 'calc(100vw - 32px)', maxHeight: '70vh', overflowY: 'auto',
                        backgroundColor: PANEL_BG, border: '1px solid #444', borderRadius: '8px',
                        padding: '16px', marginBottom: '10px', boxShadow: '0 8px 30px rgba(0,0,0,0.6)',
                        color: '#e0e0e0', textAlign: 'left',
                    }}
                >
                    <p style={{ margin: '0 0 14px 0', fontSize: '0.85rem', color: '#aaa' }}>
                        Demo reference. Barcodes and the badge are read live from the database — click any of them to copy.
                    </p>

                    <Section title="Logins">
                        {help.logins.map(login => (
                            <div key={login.username} style={{ backgroundColor: CARD_BG, borderRadius: '6px', padding: '10px', marginBottom: '8px' }}>
                                <div style={{ marginBottom: '4px' }}>
                                    <Copyable value={login.username} />
                                    <Copyable value={login.password} />
                                    <span style={{ fontSize: '0.75rem', color: '#888' }}>{login.role}</span>
                                </div>
                                <p style={{ margin: 0, fontSize: '0.8rem', color: '#bbb' }}>{login.description}</p>
                            </div>
                        ))}
                    </Section>

                    <Section title="Supervisor badge">
                        <div style={{ backgroundColor: CARD_BG, borderRadius: '6px', padding: '10px' }}>
                            {help.supervisorBadge.barcode && <Copyable value={help.supervisorBadge.barcode} />}
                            <p style={{ margin: 0, fontSize: '0.8rem', color: '#bbb' }}>{help.supervisorBadge.description}</p>
                        </div>
                    </Section>

                    <Section title="Available containers">
                        <BarcodeList
                            values={help.availableContainers}
                            emptyNote="None free right now — every container is mid-task. Finish or cancel a task to release one."
                        />
                    </Section>

                    <Section title="Conveyor barcodes">
                        <BarcodeList values={help.conveyorBarcodes} emptyNote="None seeded." />
                    </Section>

                    <Section title="Shelf locations">
                        <BarcodeList values={help.shelfLocations} emptyNote="None seeded." />
                    </Section>

                    {help.walkthroughs.map(walkthrough => (
                        <Section key={walkthrough.title} title={walkthrough.title}>
                            <ol style={{ margin: 0, paddingLeft: '18px', fontSize: '0.8rem', color: '#bbb', lineHeight: 1.5 }}>
                                {walkthrough.steps.map((step, index) => <li key={index} style={{ marginBottom: '4px' }}>{step}</li>)}
                            </ol>
                        </Section>
                    ))}
                </div>
            )}

            <button
                onClick={() => { const next = !isOpen; setIsOpen(next); if (next) refresh(); }}
                style={{
                    display: 'block', marginLeft: 'auto', padding: '10px 16px', backgroundColor: isOpen ? '#555' : '#2196F3',
                    color: 'white', border: 'none', borderRadius: '20px', cursor: 'pointer',
                    fontWeight: 'bold', fontSize: '0.9rem', boxShadow: '0 4px 12px rgba(0,0,0,0.4)',
                }}
            >
                {isOpen ? '✕ Close' : '? Demo help'}
            </button>
        </div>
    );
}
