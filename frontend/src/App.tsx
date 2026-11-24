
import { useState } from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
    faChessKnight,
    faFileImport,
    faArrowRight,
    faXmark,
    faTrophy,
    faBullseye,
    faCircleCheck,
    faTriangleExclamation,
    faSkull,
} from '@fortawesome/free-solid-svg-icons'
import './App.css'
const API_URL = 'http://192.168.1.21:5153'

interface ImportedGame {
    whitePlayer: string
    blackPlayer: string
    result: string
    totalMoves: number
    initialPosition: string
    finalPosition: string
    moves: {
        moveNumber: number
        player: string
        san: string
        uci: string
        fen: string
    }[]
    pgn: string
}

interface GameAnalysis {
    totalMoves: number
    accuracy: {
        white: number
        black: number
    }
    summary: {
        bestMoves: number
        goodMoves: number
        mistakes: number
        blunders: number
    }
}
function App() {
    const [showResults, setShowResults] = useState(false)
    const [pgn, setPgn] = useState('')
    const [isAnalyzing, setIsAnalyzing] = useState(false)
    const [error, setError] = useState('')
    const [analysis, setAnalysis] = useState<GameAnalysis | null>(null)
    const [game, setGame] = useState<ImportedGame | null>(null)
    const handleAnalyze = async () => {
        if (!pgn.trim()) return

        setIsAnalyzing(true)
        setError('')
        setAnalysis(null)
        setGame(null)
        setShowResults(false)
        try {
            // 1. Importar la partida
            const importResponse = await fetch(`${API_URL}/api/Games/import`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ pgn }),
            })

            if (!importResponse.ok) {
                throw new Error('No se pudo importar la partida.')
            }

            const importedGame: ImportedGame = await importResponse.json()

            setGame(importedGame)

            // 2. Preparar los datos para analizar
            const request = {
                initialPosition: importedGame.initialPosition,
                moves: importedGame.moves,
            }

            // 3. Enviar la partida a Stockfish
            const analysisResponse = await fetch(`${API_URL}/api/Games/analyze`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(request),
            })

            if (!analysisResponse.ok) {
                throw new Error('No se pudo analizar la partida.')
            }

            const result: GameAnalysis = await analysisResponse.json()

            setAnalysis(result)
            setShowResults(true)
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Ocurrió un error al analizar la partida.',
            )
        } finally {
            setIsAnalyzing(false)
        }
    }
    return (
        <div className="app">
            <header className="header">
                <div className="logo" >
                    <FontAwesomeIcon icon={faChessKnight} />
                    <span> Chessero </span>
                </div>

                < nav >
                    <a href="#analizar" > {isAnalyzing ? 'Analizando...' : 'Analizar partida'} </a>
                </nav>
            </header>

            < main className="main" id="analizar" >
                <section className="hero" >
                    <span className="eyebrow" > ANÁLISIS DE AJEDREZ </span>

                    <h1>
                        Analiza tu partida.
                        < br />
                        <span>Mejora tu juego.</span>
                    </h1>

                    < p className="description" >
                        Importa tu partida y descubre los mejores movimientos,
                        errores y oportunidades de mejora.
                    </p>
                </section>
                {showResults && analysis && game && (
                    <section className="results-modal">
                        <div className="modal-header">
                            <div className="modal-title">
                                <div className="modal-icon">
                                    <FontAwesomeIcon icon={faTrophy} />
                                </div>
                                <h2>Análisis completado</h2>
                            </div>

                            <button
                                className="modal-close"
                                onClick={() => setShowResults(false)}
                                aria-label="Cerrar resultados"
                            >
                                <FontAwesomeIcon icon={faXmark} />
                            </button>
                        </div>

                        <div className="game-result">
                            <span className="section-label">
                                RESULTADO DE LA PARTIDA
                            </span>

                            <div className="players-result">
                                <strong>{game.whitePlayer}</strong>
                                <span className="score">{game.result}</span>
                                <strong>{game.blackPlayer}</strong>
                            </div>

                            <div className="result-footer">
                                <span className="result-badge">
                                    {game.result === '1-0'
                                        ? 'Victoria blancas'
                                        : game.result === '0-1'
                                            ? 'Victoria negras'
                                            : 'Tablas'}
                                </span>
                                <span className="game-moves">
                                    {game.totalMoves} movimientos
                                </span>
                            </div>
                        </div>

                        <div className="accuracy-grid">
                            <div className="accuracy-card white-accuracy">
                                <div className="stat-heading">
                                    <FontAwesomeIcon icon={faBullseye} className="white-icon" />
                                    <span>Precisión blancas</span>
                                </div>
                                <strong>{analysis.accuracy.white}%</strong>
                            </div>

                            <div className="accuracy-card black-accuracy">
                                <div className="stat-heading">
                                    <FontAwesomeIcon icon={faBullseye} className="black-icon" />
                                    <span>Precisión negras</span>
                                </div>
                                <strong>{analysis.accuracy.black}%</strong>
                            </div>
                        </div>

                        <h3 className="summary-heading">
                            Resumen de movimientos
                        </h3>

                        <div className="summary-grid">
                            <div className="summary-item">
                                <FontAwesomeIcon icon={faTrophy} className="best-icon" />
                                <div className="summary-text">
                                    <span>Mejores</span>
                                    <strong>{analysis.summary.bestMoves}</strong>
                                </div>
                            </div>

                            <div className="summary-item">
                                <FontAwesomeIcon icon={faCircleCheck} className="good-icon" />
                                <div className="summary-text">
                                    <span>Buenos</span>
                                    <strong>{analysis.summary.goodMoves}</strong>
                                </div>
                            </div>

                            <div className="summary-item">
                                <FontAwesomeIcon icon={faTriangleExclamation} className="mistake-icon" />
                                <div className="summary-text">
                                    <span>Errores</span>
                                    <strong>{analysis.summary.mistakes}</strong>
                                </div>
                            </div>

                            <div className="summary-item">
                                <FontAwesomeIcon icon={faSkull} className="blunder-icon" />
                                <div className="summary-text">
                                    <span>Graves errores</span>
                                    <strong>{analysis.summary.blunders}</strong>
                                </div>
                            </div>
                        </div>

                        <button
                            className="modal-footer-btn"
                            onClick={() => setShowResults(false)}
                        >
                            Cerrar análisis
                        </button>
                    </section>
                )}

                < section className="import-card" >
                    <div className="card-heading" >
                        <div className="card-icon" >
                            <FontAwesomeIcon icon={faFileImport} />
                        </div>

                        < div >
                            <h2>Importar partida </h2>
                            < p > Pega el PGN de tu partida para comenzar.</p>
                        </div>
                    </div>

                    < textarea
                        value={pgn}
                        onChange={(event) => setPgn(event.target.value)
                        }
                        placeholder={'[Event "Player 1 vs. Player 2"]\n[White "Player 1"]\n[Black "Player 2"]\n\n1. e4 e5 2. Nf3 Nc6...'}
                    />

                    <div className="card-footer" >
                        <span>{pgn.trim().length} caracteres </span>

                        <button
                            onClick={handleAnalyze}
                            disabled={!pgn.trim() || isAnalyzing}
                        >
                            {isAnalyzing ? 'Analizando...' : 'Analizar partida'}
                            < FontAwesomeIcon icon={faArrowRight} />
                        </button>
                    </div>
                </section>

                {error && (
                    <p className="error-message">{error}</p>
                )}



                


            </main>

            < footer className="footer" >
                Chessero © 2026 · Todos los derechos reservados · Hecho por Alee7852 Chess.com
            </footer>
        </div>
    )
}

export default App
