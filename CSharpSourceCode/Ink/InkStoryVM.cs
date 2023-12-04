using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.Ink
{
    public class InkStoryVM : ViewModel
    {
        private InkStory _story;
        private MBBindingList<InkStoryChoiceVM> _choices = new MBBindingList<InkStoryChoiceVM>();
        private bool _isVisible = false;
        private string _title = "Title";
        private string _currentText = string.Empty;
        private int _maxChars = 1200;
        private string _spritePath = string.Empty;

        public void SetStory(InkStory story)
        {
            CurrentText = string.Empty;
            _story = story;
            _story.Reset();
            _story.SetTitle();
            SpritePath = story.GetInitialIllustration();
            _story.Continue(out _);
            RefreshValues();
        }

        public override void RefreshValues()
        {
            Title = _story.Title;
            if (_story == null) return;
            //Get Sprite to display on the right
            PlaySound();
            if (_story.GetCurrentIllustration() != null)
            {
                SpritePath = _story.GetCurrentIllustration();
            }
           
            //Calc if the current text is too long
            if(CurrentText.Count() > _maxChars)
            {
                CurrentText = _story.GetLine();
            }
            else
            {
                CurrentText = CurrentText + "\n" + _story.GetLine();
                
            }
            //Check choices
            _choices.Clear();
            if (_story.IsOver())
            {
                _choices.Add(new InkStoryChoiceVM(-1, new TextObject ("{=inky_end_str}End").ToString(), OnChoiceSelected));
            }
            else if (!_story.HasChoices())
            {
                
                _choices.Add(new InkStoryChoiceVM(0, new TextObject ("{=inky_continue_str}Continue").ToString(), OnChoiceSelected));
            }
            else
            {
                foreach(var choice in _story.GetChoices())
                {
                    var text = _story.getChoiceText (choice);
                    _choices.Add(new InkStoryChoiceVM(choice.index, text, OnChoiceSelected));
                }
            }
        }

        private void OnChoiceSelected(int index)
        {
            if (index == -1)
            {
                InkStoryManager.CloseCurrentStory();
                return;
            }
            else if (_story.HasChoices())
            {
                CurrentText = string.Empty;
                _story.ChooseChoice(index);
            }
            _story.Continue(out _);
            RefreshValues();
        }

        private void PlaySound()
        {
            int number = MBRandom.RandomInt(1, 3);
            SoundEvent.PlaySound2D("scribble" + number);
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }

        [DataSourceProperty]
        public string CurrentText
        {
            get
            {
                return _currentText;
            }
            set
            {
                if (value != _currentText)
                {
                    _currentText = value;
                    OnPropertyChangedWithValue(value, "CurrentText");
                }
            }
        }

        [DataSourceProperty]
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    OnPropertyChangedWithValue(value, "Title");
                }
            }
        }

        [DataSourceProperty]
        public string SpritePath
        {
            get
            {
                return _spritePath;
            }
            set
            {
                if (value != _spritePath)
                {
                    _spritePath = value;
                    OnPropertyChangedWithValue(value, "SpritePath");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<InkStoryChoiceVM> CurrentChoices
        {
            get
            {
                return _choices;
            }
            set
            {
                if (value != _choices)
                {
                    _choices = value;
                    OnPropertyChangedWithValue(value, "CurrentChoices");
                }
            }
        }

        public void ClearStory()
        {
            _story = null;
        }

        public override void OnFinalize()
        {
            ClearStory();
        }
    }
}
