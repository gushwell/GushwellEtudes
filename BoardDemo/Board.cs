using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;

// WindowsFormsで利用する場合は、System.Windows.Media の代わりに、
// System.Drawing を using する。

namespace Gushwell.Etude {
    // Changeイベントで使われる EventArgs
    public class BoardChangedEventArgs : EventArgs {
        public Location Location { get; internal set; }
        public IPiece Piece { get; internal set; }
    }

    // 盤データクラス
    public class Board {
        // 駒が配置される１次元配列 （周辺には番兵が置かれる）
        private IPiece[] _pieces;

        // 番兵以外の有効な位置(１次元のインデックス）が格納される
        private readonly int[] _validIndexes;

        // 盤の行（縦方向）数
        public int YSize { get; private set; }
        // 盤のカラム（横方向）数
        public int XSize { get; private set; }

        // _pieces配列に変更があるとChangeイベントが発生する。
        public event EventHandler<BoardChangedEventArgs> Changed;

        // コンストラクタ
        public Board(int xsize, int ysize) {
            this.YSize = ysize;
            this.XSize = xsize;
            // 盤データの初期化 （周りは番兵(Guard)をセットしておく）
            _pieces = new IPiece[(xsize + 2) * (ysize + 2)];
            for (int i = 0; i < _pieces.Length; i++) {
                if (IsOnBoard(ToLocation(i)))    // この時点でIsOnBoard(int index) は利用できない
                    _pieces[i] = Pieces.Empty;
                else
                    _pieces[i] = Pieces.Guard;
            }
            // 毎回求めるのは無駄なので最初に求めておく
            _validIndexes = Enumerable.Range(0, _pieces.Length)
                                     .Where(ix => _pieces[ix] == Pieces.Empty)
                                     .ToArray();

        }

        // コンストラクタ (Cloneとしても利用できる)
        public Board(Board board) {
            this.YSize = board.YSize;
            this.XSize = board.XSize;
            this._validIndexes = board._validIndexes.ToArray();
            this._pieces = board._pieces.ToArray();
        }

        // イベント発行 
        protected void OnChanged(Location loc, IPiece piece) {
            if (Changed != null) {
                var args = new BoardChangedEventArgs {
                    Location = loc,
                    Piece = piece
                };
                Changed(this, args);
            }
        }

        // (x,y) から、_pieceへのインデックスを求める
        public int ToIndex(int x, int y) {
            return x + y * (XSize + 2);
        }

        // Location から _pieceのIndexを求める
        public int ToIndex(Location loc) {
            return ToIndex(loc.X, loc.Y);
        }

        // IndexからLocationを求める
        public Location ToLocation(int index) {
            return new Location(index % (XSize + 2), index / (XSize + 2));
        }

        // 本来のボード上の位置かどうかを調べる
        protected bool IsOnBoard(Location loc) {
            int x = loc.X;
            int y = loc.Y;
            return ((1 <= x && x <= XSize) &&
                   (1 <= y && y <= YSize));
        }

        // 本来のボード上の位置(index)かどうかを調べる
        protected bool IsOnBoard(int index) {
            if (0 <= index && index < _pieces.Length)
                return this[index] != Pieces.Guard;
            return false;
        }

        // Pieceを置く_piecesの要素を変更するのはこのメソッドだけ（コンストラクタは除く）。
        // override可 
        protected virtual void PutPiece(int index, IPiece piece) {
            if (IsOnBoard(index)) {
                _pieces[index] = piece;
                OnChanged(ToLocation(index), piece);
            } else {
                throw new ArgumentOutOfRangeException();
            }
        }

        // インデクサ (x,y)の位置の要素へアクセスする
        public IPiece this[int index]
        {
            get { return _pieces[index]; }
            set { PutPiece(index, value); }
        }

        // インデクサ (x,y)の位置の要素へアクセスする
        public IPiece this[int x, int y]
        {
            get { return this[ToIndex(x, y)]; }
            set { this[ToIndex(x, y)] = value; }
        }

        // インデクサ locの位置の要素へアクセスする
        public IPiece this[Location loc]
        {
            get { return this[loc.X, loc.Y]; }
            set { this[loc.X, loc.Y] = value; }
        }

        // 全てのPieceをクリアする
        public virtual void ClearAll() {
            foreach (var ix in GetOccupiedIndexes())
                ClearPiece(ToLocation(ix));
        }

        // x,yの位置をクリアする
        public virtual void ClearPiece(Location loc) {
            this[loc.X, loc.Y] = Pieces.Empty;
        }

        // EmptyPiece 以外の全てのPieceを列挙する
        public IEnumerable<IPiece> GetAllPieces() {
            return _validIndexes.Select(i => this[i]).Where(p => p != Pieces.Empty);
        }

        // 指定したIPieceが置いてあるLocationを列挙する
        public IEnumerable<Location> GetLocations(IPiece piece) {
            Type type = piece.GetType();
            return GetValidLocations().Where(loc => this[loc].GetType() == type);
        }

        // 指定したIPieceがおいてあるIndexを列挙する
        public IEnumerable<int> GetIndexes(IPiece piece) {
            Type type = piece.GetType();
            return _validIndexes.Where(index => this[index].GetType() == type);
        }

        // 番兵部分を除いた有効なLocationを列挙する
        public IEnumerable<Location> GetValidLocations() {
            return _validIndexes.Select(ix => ToLocation(ix));
        }

        // 番兵部分を除いた有効なIndexを列挙する
        public IEnumerable<int> GetValidIndexes() {
            return _validIndexes;
        }

        // 駒が置かれているLocationを列挙する
        public IEnumerable<Location> GetOccupiedLocations() {
            return GetOccupiedIndexes().Select(index => ToLocation(index));
        }

        // 駒が置かれているLocationを列挙する
        public IEnumerable<int> GetOccupiedIndexes() {
            return _validIndexes.Where(index => this[index] != Pieces.Empty);
        }

        // 何もおかれていないLocationを列挙する
        public IEnumerable<Location> GetVacantLocations() {
            return _validIndexes.Select(index => ToLocation(index));
        }

        // 何もおかれていないIndexを列挙する
        public IEnumerable<int> GetVacantIndexes() {
            return GetIndexes(Pieces.Empty);
        }

        // 指定した方向の位置を番兵が見つかるまで取得する。
        public IEnumerable<int> GetSeriesIndexes(int index, int direction) {
            for (int pos = index; this[pos] != Pieces.Guard; pos += direction)
                yield return pos;
        }

        // 上方向
        public int UpDirection
        {
            get { return -(this.XSize + 2); }
        }

        // 下方向
        public int DownDirection
        {
            get { return (this.XSize + 2); }
        }

        // 左方向
        public int LeftDirection
        {
            get { return -1; }
        }

        // 右方向
        public int RightDirection
        {
            get { return 1; }
        }

        // 右上の方向
        public int UpperRightDirection
        {
            get { return UpDirection + 1; }
        }

        // 左上の方向
        public int UpperLeftDirection
        {
            get { return UpDirection - 1; }
        }

        // 右下の方向
        public int LowerRightDirection
        {
            get { return DownDirection + 1; }
        }

        // 左下の方向
        public int LowerLeftDirection
        {
            get { return DownDirection - 1; }
        }
    }

    // 駒を示すマーカーインターフェース(marker interface)
    public interface IPiece {
    }

    // 色を持つ駒
    public interface IColorPiece : IPiece {
        Color Color { get; }
    }

    // 良く利用する駒たち
    public static class Pieces {
        public static readonly IPiece Black = new BlackPiece();
        public static readonly IPiece White = new WhitePiece();
        public static readonly IPiece Empty = new EmptyPiece();
        public static readonly IPiece Guard = new GuardPiece();
    }

    // 黒石
    public struct BlackPiece : IColorPiece {
        public Color Color
        {
            get { return Color.FromArgb(255, 128, 128, 128); }
        }
    }

    // 白石
    public struct WhitePiece : IColorPiece {
        public Color Color
        {
            get { return Color.FromArgb(255, 255, 255, 255); }
        }
    }

    // 番兵用：他と区別するためだけなので中身は何でも良い (マーカーオブジェクト)
    public struct GuardPiece : IPiece {
    }

    // 何もおかれていないことを示す：いわゆる NullObject  (マーカーオブジェクト)
    public struct EmptyPiece : IPiece {
    }

    // ボード上の位置を示す
    public class Location {
        public int X { get; set; }
        public int Y { get; set; }
        public Location(int x, int y) {
            X = x;
            Y = y;
        }
        public override string ToString() {
            return string.Format("({0},{1}) ", X, Y);
        }
    }
}