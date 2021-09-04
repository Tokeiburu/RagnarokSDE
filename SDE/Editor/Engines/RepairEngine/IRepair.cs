using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using System.Windows.Controls;
using TokeiLibrary;

namespace SDE.Editor.Engines.RepairEngine
{
    public static class RepairHelper
    {
        #region Delegates

        public delegate bool RepairActionDelegate(BaseDb db);

        #endregion Delegates
    }

    public abstract class AbstractRepair : IRepair
    {
        public object DataImage
        {
            get
            {
                if (ImagePath == null)
                    return null;
                return ApplicationManager.PreloadResourceImage(ImagePath);
            }
        }

        public abstract RepairHelper.RepairActionDelegate RepairMethod { get; }

        #region IRepair Members

        public abstract string ImagePath { get; set; }
        public abstract string DisplayName { get; set; }

        public abstract bool Show(BaseDb db);

        public abstract bool CanRepair(BaseDb db);

        public bool Repair(BaseDb db)
        {
            return RepairMethod(db);
        }

        #endregion IRepair Members

        protected abstract bool _repair(BaseDb db, RepairHelper.RepairActionDelegate repair);

        public virtual object Select(out ServerDbs db)
        {
            db = null;
            return null;
        }

        public ContextMenu GenerateContextMenu(BaseDb db)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem mItem;

            if (CanRepair(db))
            {
                mItem = new MenuItem();
                mItem.Header = "Fix";
                mItem.Icon = ApplicationManager.GetResourceImage("validity.png");
                mItem.Click += delegate { Repair(db); };
                menu.Items.Add(mItem);
            }

            return menu;
        }
    }

    public interface IRepair
    {
        string ImagePath { get; set; }
        string DisplayName { get; set; }

        bool Show(BaseDb db);

        bool CanRepair(BaseDb db);

        bool Repair(BaseDb db);
    }
}