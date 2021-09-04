using System.Windows.Controls;
using TokeiLibrary;

namespace SDE.View.Controls
{
    public class ClickSelectTextBoxEffect : ClickSelectTextBox
    {
        public ClickSelectTextBoxEffect()
        {
            this.Loaded += delegate
            {
                this.BorderThickness = new System.Windows.Thickness(0);
                //this.Padding = new System.Windows.Thickness(0);

                if (this.Parent is Border)
                    WpfUtilities.AddFocus(this);
                else
                {
                    Panel parent = this.Parent as Panel;

                    if (parent != null)
                    {
                        int oldPosition = parent.Children.IndexOf(this);
                        parent.Children.Remove(this);

                        Border border = new Border();
                        border.VerticalAlignment = this.VerticalAlignment;
                        border.HorizontalAlignment = this.HorizontalAlignment;
                        border.BorderThickness = new System.Windows.Thickness(1);
                        border.Child = this;
                        border.Margin = this.Margin;
                        this.Margin = new System.Windows.Thickness(0);
                        parent.Children.Insert(oldPosition, border);
                        WpfUtilities.SetGridPosition(border, (int)this.GetValue(Grid.RowProperty), (int)this.GetValue(Grid.ColumnProperty));
                        WpfUtilities.AddFocus(this);
                    }
                }
            };
        }
    }
}