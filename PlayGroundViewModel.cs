using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace synchronizationContext.View_Models
{
    public class PlayGroundViewModel :ViewModelBase
    {
        ObservableCollection<SnakeBlockViewModel> _snake=new ObservableCollection<SnakeBlockViewModel>();

        public PlayGroundViewModel()
        {
         Snake.Add(new SnakeBlockViewModel() {IsHead = true});
         Snake.Add(new SnakeBlockViewModel() {IsHead = false});

        }
        public ObservableCollection<SnakeBlockViewModel> Snake
        {
            get { return _snake; }
            set
            {
                _snake = value;
                OnPropertyChanged(nameof(Snake));
            }
        }
    }
}
