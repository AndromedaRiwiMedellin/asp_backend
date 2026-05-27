import { Html5QrcodeScanner } from "html5-qrcode";
import { useEffect, useRef, useState } from "react";
import { scanTicket } from "../api";

const SCANNER_REGION_ID = "scanner-region";

export function ScannerPage({ token, eventId, session }) {
  const [manualCode, setManualCode] = useState("");
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const lastCodeRef = useRef("");
  const [loading, setLoading] = useState(false);
  const scannerRef = useRef(null);

  const sendScan = async (qrCode) => {
    const normalized = qrCode.trim();
    if (!normalized) {
      return;
    }

    setLoading(true);
    setError("");

    try {
      const payload = await scanTicket(token, normalized, eventId);
      setResult(payload);
      lastCodeRef.current = normalized;
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const scanner = new Html5QrcodeScanner(
      SCANNER_REGION_ID,
      {
        fps: 10,
        qrbox: { width: 240, height: 240 },
        rememberLastUsedCamera: true,
      },
      false
    );

    let handlingScan = false;

    scanner.render(
      async (decodedText) => {
        if (handlingScan) {
          return;
        }

        if (decodedText === lastCodeRef.current) {
          return;
        }

        handlingScan = true;
        await sendScan(decodedText);
        setTimeout(() => {
          handlingScan = false;
        }, 1000);
      },
      () => {
        // noise from frame decode failures is expected
      }
    );

    scannerRef.current = scanner;

    return () => {
      scanner
        .clear()
        .catch(() => {
          // cleanup fallback
        });
    };
  }, [token, eventId]);

  const submitManual = async (event) => {
    event.preventDefault();
    await sendScan(manualCode);
    setManualCode("");
  };

  const sessionEventName = session?.activeEvent?.eventTitle ?? "Sin evento";

  return (
    <section className="content-grid">
      <article className="card">
        <h2>Sesión activa</h2>
        <p className="muted">Usuario: {session?.userEmail ?? "-"}</p>
        <p className="muted">Rol: {session?.role ?? "-"}</p>
        <p className="muted">Evento: {sessionEventName}</p>
      </article>

      <article className="card">
        <h2>Escáner</h2>
        <p className="muted">Abre esta página desde el celular para escanear con cámara, o usa tu lector físico en el campo manual.</p>
        <div id={SCANNER_REGION_ID} className="scanner-region" />
      </article>

      <article className="card">
        <h2>Escaneo manual / lector físico</h2>
        <form className="form" onSubmit={submitManual}>
          <label htmlFor="manual-code">Código QR o serial</label>
          <input
            id="manual-code"
            type="text"
            value={manualCode}
            onChange={(e) => setManualCode(e.target.value)}
            placeholder="Ej: ANDRO-VALID-001"
            autoFocus
          />
          <button type="submit" disabled={loading || !manualCode.trim()}>
            {loading ? "Validando..." : "Validar ticket"}
          </button>
        </form>

        {error ? <p className="error-text">{error}</p> : null}

        {result ? (
          <div className={`scan-result ${result.success ? "ok" : "bad"}`}>
            <h3>{result.success ? "Ingreso autorizado" : "Ingreso denegado"}</h3>
            <p>{result.message}</p>
            <p><strong>Razón:</strong> {result.reason}</p>
            {result.ticket ? (
              <ul>
                <li>Propietario: {result.ticket.ownerName ?? "Sin dato"}</li>
                <li>Email: {result.ticket.ownerEmail ?? "Sin dato"}</li>
                <li>Evento: {result.ticket.eventTitle ?? "Sin dato"}</li>
                <li>Silla: {result.ticket.seatNumber ?? "Sin dato"}</li>
                <li>Estado: {result.ticket.status ?? "Sin dato"}</li>
              </ul>
            ) : null}
          </div>
        ) : null}
      </article>
    </section>
  );
}
