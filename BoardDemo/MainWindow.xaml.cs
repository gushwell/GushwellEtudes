using Gushwell.Etude;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BoardDemo {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private BoardCanvas _bcanvas;
        private Board _board;

        private void canvas1_Loaded(object sender, RoutedEventArgs e) {
            _board = new Board(10, 10);
            _bcanvas = new BoardCanvas(this.canvas1, _board);
            _bcanvas.DrawRuledLines(BoardType.Chess);
        }

        // クリックされるたびに、黒 → 白 → 消去 を繰り返す。
        private void canvas1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Location loc = _bcanvas.ToLocation(e.GetPosition(this.canvas1 as UIElement));
            var piece = _board[loc];
            if (piece is EmptyPiece) {
                _board[loc] = Pieces.Black;
            } else if (piece is BlackPiece) {
                _board[loc] = Pieces.White;
            } else {
                _board[loc] = Pieces.Empty;
            }
        }

        // 全てをクリアする
        private void button1_Click(object sender, RoutedEventArgs e) {
            _board.ClearAll();
        }

        // 空いている箇所を白で埋め尽くす
        private void button2_Click(object sender, RoutedEventArgs e) {
            foreach (var index in _board.GetVacantIndexes()) {
                _board[index] = Pieces.White;
            }
        }

        // 白の数をカウントする
        private void button3_Click(object sender, RoutedEventArgs e) {
            int count = _board.GetIndexes(Pieces.White).Count();
            textBlock1.Text = string.Format("白の数={0}", count);
        }
    }
}

