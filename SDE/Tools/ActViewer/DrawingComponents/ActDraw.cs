using GRF.FileFormats.ActFormat;
using System.Collections.Generic;

namespace SDE.Tools.ActViewer.DrawingComponents
{
    /// <summary>
    /// Drawing component for an Act object.
    /// </summary>
    public class ActDraw : DrawingComponent
    {
        private readonly Act _act;
        private readonly List<DrawingComponent> _components = new List<DrawingComponent>();
        private bool _componentsInitiated;
        private readonly IPreview _preview;

        public ActDraw(Act act)
        {
            _act = act;
        }

        public ActDraw(Act act, IPreview preview)
        {
            _act = act;
            _preview = preview;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ActDraw" /> is the act currently being edited.
        /// </summary>
        public bool Primary
        {
            get { return _act.Name == null; }
        }

        /// <summary>
        /// Gets a list of all the components being drawn.
        /// </summary>
        public List<DrawingComponent> Components
        {
            get { return _components; }
        }

        public override void Render(IPreview frameEditor)
        {
            if (!_componentsInitiated)
            {
                int actionIndex = frameEditor.SelectedAction;
                int frameIndex = frameEditor.SelectedFrame;

                if (actionIndex >= _act.NumberOfActions) return;
                if (frameIndex >= _act[actionIndex].NumberOfFrames)
                {
                    if (Primary)
                        return;

                    frameIndex = frameIndex % _act[actionIndex].NumberOfFrames;
                }

                Frame frame = _act[actionIndex, frameIndex];

                for (int i = 0; i < frame.NumberOfLayers; i++)
                {
                    var layer = new LayerDraw(_preview, _act, i);

                    if (Primary)
                    {
                        layer.Selected += (s, e, a) => OnSelected(e, a);
                    }

                    Components.Add(layer);
                }

                _componentsInitiated = true;
            }

            foreach (var dc in Components)
            {
                if (Primary)
                {
                    dc.IsSelectable = true;
                }

                dc.Render(frameEditor);
            }
        }

        public override void QuickRender(IPreview frameEditor)
        {
            foreach (var dc in Components)
            {
                dc.QuickRender(frameEditor);
            }
        }

        public override void Remove(IPreview frameEditor)
        {
            foreach (var dc in Components)
            {
                dc.Remove(frameEditor);
            }
        }

        public void Render(IPreview frameEditor, int layerIndex)
        {
            Components[layerIndex].Render(frameEditor);
        }

        public override void Select()
        {
            foreach (var comp in Components)
            {
                comp.Select();
            }
        }

        public void Select(int layer)
        {
            if (layer > -1 && layer < Components.Count)
            {
                Components[layer].Select();
            }
        }

        public void Deselect(int layer)
        {
            if (layer > -1 && layer < Components.Count)
            {
                Components[layer].IsSelected = false;
            }
        }

        public void Deselect()
        {
            foreach (LayerDraw sd in Components)
            {
                sd.IsSelected = false;
            }
        }

        public LayerDraw Get(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= Components.Count) return null;

            return Components[layerIndex] as LayerDraw;
        }
    }
}