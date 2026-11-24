
import { useState } from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
    faChessKnight,
    faFileImport,
    faMagnifyingGlass,
    faArrowRight,
} from '@fortawesome/free-solid-svg-icons'
import './App.css'

function App() {
    const [pgn, setPgn] = useState('')

    return (
        <div className= "app">
        <header className="header">
            <div className="logo" >
                <FontAwesomeIcon icon={ faChessKnight }/>
                    <span> Chessero </span>
                    </div>

                    < nav >
                    <a href="#analizar" > Analizar partida </a>
                        </nav>
                        </header>

                        < main className = "main" id = "analizar" >
                            <section className="hero" >
                                <span className="eyebrow" > ANÁLISIS DE AJEDREZ </span>

                                    <h1>
            Analiza tu partida.
            < br />
        <span>Mejora tu juego.</span>
            </h1>

            < p className = "description" >
                Importa tu partida y descubre los mejores movimientos,
                    errores y oportunidades de mejora.
          </p>
                        </section>

                        < section className = "import-card" >
                            <div className="card-heading" >
                                <div className="card-icon" >
                                    <FontAwesomeIcon icon={ faFileImport } />
                                        </div>

                                        < div >
                                        <h2>Importar partida </h2>
                                            < p > Pega el PGN de tu partida para comenzar.</p>
                                                </div>
                                                </div>

                                                < textarea
    value = { pgn }
    onChange = {(event) => setPgn(event.target.value)
}
                        placeholder={ '[Event "Player 1 vs. Player 2"]\n[White "Player 1"]\n[Black "Player 2"]\n\n1. e4 e5 2. Nf3 Nc6...'}
    />

    <div className="card-footer" >
        <span>{ pgn.trim().length } caracteres </span>

            < button disabled = {!pgn.trim()}>
                <FontAwesomeIcon icon={ faMagnifyingGlass } />
              Analizar partida
    < FontAwesomeIcon icon = { faArrowRight } />
        </button>
        </div>
        </section>
        </main>

        < footer className = "footer" >
                Chessero © 2026 · Todos los derechos reservados · Hecho por Alee7852 Chess.com
                </footer>
                </div>
  )
}

export default App
