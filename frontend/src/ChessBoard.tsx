import { Chessboard } from 'react-chessboard'

interface ChessBoardProps {
    position: string
}

function ChessBoard({ position }: ChessBoardProps) {
    return (
        <div className="chessboard-container">
            <Chessboard
                options={{
                    position: position,
                    allowDragging: false,
                }}
            />
        </div>
    )
}

export default ChessBoard
