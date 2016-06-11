using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Gushwell.Etude;

namespace Gushwell.Etude {
    // 罫線の種類
    public enum BoardType {
        Go,
        Chess
    }

    // Boardおよび駒(PIece)の表示を担当する
    // BoardオブジェクトからChangeイベントを受け取ると、変更されたセルのPieceを描画する。
    // その他Board/Pieceを描画するための各種メソッドを用意。
    // PanelにUiElementを動的に追加削除することで描画を行っている。
    public class BoardCanvas {
        protected double CellWidth { get; private set; }        // ひとつのCellの幅
        protected double CellHeight { get; private set; }       // ひとつのCellの高さ
        protected int YSize { get; private set; }            // 縦方向のCellの数
        protected int XSize { get; private set; }            // 横方向のCellの数
        protected BoardType BoardType { get; private set; }     // 罫線の種類 (碁盤かチェス盤か）
        protected Panel Panel { get; private set; }             // 対象となる Panelオブジェクト
        protected Board Board { get; private set; }             // 対象となる Boardオブジェクト

        // コンストラクタ
        public BoardCanvas(Panel panel, Board board) {
            this.Board = board;
            this.Panel = panel;
            YSize = board.YSize;
            XSize = board.XSize;

            CellWidth = (panel.ActualWidth - 1) / XSize;
            CellHeight = (panel.ActualHeight - 1) / YSize;
            foreach (var loc in board.GetValidLocations()) {
                UpdatePiece(loc, board[loc]);
            }
            this.Board.Changed += new EventHandler<BoardChangedEventArgs>(board_Changed);
            _synchronize = true;
        }

        private bool _synchronize;

        // Boardオブジェクトと同期するか否か （初期値：同期する）
        public bool Synchronize
        {
            get { return _synchronize; }
            set
            {
                if (value == true) {
                    if (!_synchronize) {
                        this.Board.Changed += new EventHandler<BoardChangedEventArgs>(board_Changed);
                        _synchronize = true;
                    }
                } else {
                    if (_synchronize) {
                        this.Board.Changed -= new EventHandler<BoardChangedEventArgs>(board_Changed);
                        _synchronize = false;
                    }
                }
            }
        }

        public void ChangeBoard(Board board) {
            this.Board = board;
        }


        // 罫線を引く
        public void DrawRuledLines(BoardType linetype) {
            this.BoardType = linetype;
            int startx = (linetype == BoardType.Chess)
                            ? 0
                            : (int)(CellWidth / 2);

            for (double i = startx; i <= Panel.ActualWidth; i += CellWidth) {
                DrawLine(i, 0, i, Panel.ActualHeight);
            }
            int starty = (linetype == BoardType.Chess)
                            ? 0
                            : (int)(CellHeight / 2);
            for (double i = starty; i <= Panel.ActualHeight; i += CellHeight) {
                DrawLine(0, i, Panel.ActualWidth, i);
            }


        }

        // 線を引く
        protected void DrawLine(double x1, double y1, double x2, double y2) {
            Line line = new Line();
            line.X1 = x1;
            line.Y1 = y1;
            line.X2 = x2;
            line.Y2 = y2;
            line.Stroke = new SolidColorBrush(Colors.LightGray);
            Panel.Children.Add(line);
        }

        // 円オブジェクトを生成する （Pieceのデフォルト表示を担当)
        public Ellipse CreateEllipse(Location loc, Color color) {
            Ellipse eli = new Ellipse();
            eli.Name = PieceName(loc);
            eli.Width = CellWidth * 0.85;
            eli.Height = CellHeight * 0.85;
            eli.Fill = new SolidColorBrush(color);
            eli.Stroke = new SolidColorBrush(Colors.DarkGray);
            Point pt = ToPoint(loc);
            Canvas.SetLeft(eli, pt.X + CellWidth / 2 - eli.Width / 2);
            Canvas.SetTop(eli, pt.Y + CellHeight / 2 - eli.Height / 2);
            return eli;
        }

        // Pieceオブジェクトの名前を生成する
        public string PieceName(Location loc) {
            return string.Format("x{0}y{1}", loc.X, loc.Y);
        }

        // 矩形を描く
        public void DrawRectangle(Point p1, Point p2, Color color) {
            this.Panel.Children.Add(CreateRectangle(p1, p2, color));
        }

        // 矩形を消去する
        public void EraseRectangles(Point p1, Point p2) {
            var name = RectangleName(p1);
            Rectangle obj = Panel.FindName(name) as Rectangle;
            if (obj != null) {
                Panel.Children.Remove(obj as UIElement);
            }
        }

        // 四角形を生成する
        public Rectangle CreateRectangle(Point p1, Point p2, Color color) {
            var rect = new Rectangle();
            rect.Name = RectangleName(p1);
            rect.Stroke = new SolidColorBrush(color);
            double x1 = Math.Min(p1.X, p2.X);
            double y1 = Math.Min(p1.Y, p2.Y);
            double x2 = Math.Max(p1.X, p2.X);
            double y2 = Math.Max(p1.Y, p2.Y);
            rect.Margin = new Thickness(x1, y1, x2, y2);
            rect.Width = x2 - x1;
            rect.Height = y2 - y1;
            return rect;
        }

        // 矩形の名前を得る
        protected string RectangleName(Point p1) {
            return string.Format("r{0}{1}", (int)p1.X, (int)p1.Y);
        }

        // Boardの内容に沿って、Pieceを描画しなおす
        // UIスレッドとは別スレッドで動作していた場合を考慮。
        public void Invalidate() {
            Panel.Dispatcher.BeginInvoke(new Action(() => {
                foreach (var loc in Board.GetValidLocations()) {
                    this.RemovePiece(loc);
                }
                foreach (var loc in Board.GetValidLocations()) {
                    IPiece piece = Board[loc];
                    UpdatePiece(loc, piece);
                }
            }));
        }

        // UIスレッドとは別スレッドで動作させている時だけ意味を持つ。
        public TimeSpan UpdateInterval { get; set; }

        // Boardオブジェクトが変更されたときに呼び出されるイベントハンドラ
        // UIスレッドとは別スレッドで動作していた場合を考慮。
        private void board_Changed(object sender, BoardChangedEventArgs e) {
            Thread.Sleep(UpdateInterval);
            Panel.Dispatcher.BeginInvoke(new Action(() => {
                UpdatePiece(e.Location, e.Piece);
            }));
        }

        // Pieceの描画を更新する
        public virtual void UpdatePiece(Location loc, IPiece piece) {
            if (piece == null || piece is EmptyPiece) {
                RemovePiece(loc);
            } else {
                RemovePiece(loc);
                if (piece is EmptyPiece || piece is GuardPiece)
                    return;
                DrawPiece(loc, piece);
            }
        }

        // Pieceを描く。 デフォルト実装は、IColorPieceのみに対応。
        // 他のPiece型は、独自に当メソッドをoverrideする必要がある。
        // なお、overrideした DrawPiece内では、DrawRectangleメソッドは利用できない。
        public virtual void DrawPiece(Location loc, IPiece piece) {
            var colorPiece = piece as IColorPiece;
            if (colorPiece != null) {
                var name = PieceName(loc);
                var ellipse = CreateEllipse(loc, colorPiece.Color);
                Panel.Children.Add(ellipse);
                Panel.RegisterName(name, ellipse);
            }
        }

        // 指定した位置のPieceを取り除く
        public void RemovePiece(Location loc) {
            var name = PieceName(loc);
            object obj = Panel.FindName(name);
            if (obj != null) {
                Panel.Children.Remove(obj as UIElement);
                Panel.UnregisterName(name);
            }
        }

        // Locationからグラフィックの座標であるPointへ変換
        public Point ToPoint(Location loc) {
            return new Point {
                X = CellWidth * (loc.X - 1),
                Y = CellHeight * (loc.Y - 1)
            };
        }

        // グラフィックの座標であるPointからLocationへ変換
        public Location ToLocation(Point pt) {
            var x = pt.X;
            var y = pt.Y;
            int a = Math.Max(0, (int)(x / CellWidth));
            if (a >= XSize)
                a = XSize - 1;
            int b = Math.Max(0, (int)(y / CellHeight));
            if (b >= YSize)
                b = YSize - 1;
            return new Location(a + 1, b + 1);
        }
    }
}