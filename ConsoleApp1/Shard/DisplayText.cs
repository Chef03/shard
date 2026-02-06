/*
*
*   The baseline functionality for getting text to work via SDL.   You could write your own text 
*       implementation (and we did that earlier in the course), but bear in mind DisplaySDL is built
*       upon this class.
*   @author Michael Heron
*   @version 1.0
*   
*   Contributions to the code made by others:
*   @author Kyle Agius (see Changelog for 1.3.0)
*/

using SDL;
using static SDL.SDL3;
using static SDL.SDL3_ttf;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shard
{

    // We'll be using SDL3 here to provide our underlying graphics system.
    unsafe class TextDetails
    {
        string text;
        double x, y;
        SDL_Color col;
        int size;
        TTF_Font* font;
        SDL_Texture* lblText;


        public TextDetails(string text, double x, double y, SDL_Color col, int spacing)
        {
            this.text = text;
            this.x = x;
            this.y = y;
            this.col = col;
            this.size = spacing;
        }

        public string Text
        {
            get => text;
            set => text = value;
        }
        public double X
        {
            get => x;
            set => x = value;
        }
        public double Y
        {
            get => y;
            set => y = value;
        }
        public SDL_Color Col
        {
            get => col;
            set => col = value;
        }
        public int Size
        {
            get => size;
            set => size = value;
        }
        public TTF_Font* Font { get => font; set => font = value; }
        public SDL_Texture* LblText { get => lblText; set => lblText = value; }
    }

    unsafe class DisplayText : Display
    {
        protected SDL_Window* _window;
        protected SDL_Renderer* _rend;
        SDL_PixelFormat _format;
        SDL_TextureAccess _access;
        private List<TextDetails> myTexts;
        private Dictionary<string, nint> fontLibrary;
        public override void clearDisplay()
        {
            foreach (TextDetails td in myTexts)
            {
                SDL_DestroyTexture(td.LblText);
            }

            myTexts.Clear();
            SDL_SetRenderDrawColor(_rend, 0, 0, 0, 255);
            SDL_RenderClear(_rend);

        }

        public TTF_Font* loadFont(string path, int size)
        {
            string key = path + "," + size;

            if (fontLibrary.ContainsKey(key))
            {
                return (TTF_Font*)fontLibrary[key];
            }

            fontLibrary[key] = (nint)TTF_OpenFont(path, size);
            return (TTF_Font*)fontLibrary[key];
        }

        private void update()
        {


        }

        private void draw()
        {

            foreach (TextDetails td in myTexts)
            {

                SDL_FRect sRect;

                sRect.x = (float)td.X;
                sRect.y = (float)td.Y;
                sRect.w = 0;
                sRect.h = 0;


                int w, h;
                TTF_GetStringSize(td.Font, td.Text, 0, &w, &h);
                sRect.w = w;
                sRect.h = h;
                SDL_RenderTexture(_rend, td.LblText, null, &sRect);

            }

            SDL_RenderPresent(_rend);

        }

        public override void display()
        {

            update();
            draw();
        }

        public override void setFullscreen()
        {
            SDL_SetWindowFullscreen(_window, true);
        }

        public override void initialize()
        {
            fontLibrary = new Dictionary<string, nint>();

            setSize(1280, 864);

            SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_AUDIO);
            TTF_Init();
            _window = SDL_CreateWindow("Shard Game Engine",
                (int)getWidth(),
                (int)getHeight(),
                0);

            SDL_SetWindowPosition(_window, (int)SDL_WINDOWPOS_CENTERED, (int)SDL_WINDOWPOS_CENTERED);

            _rend = SDL_CreateRenderer(_window, (byte*)null);


            SDL_SetRenderDrawBlendMode(_rend, SDL_BlendMode.SDL_BLENDMODE_BLEND);

            SDL_SetRenderDrawColor(_rend, 0, 0, 0, 255);


            myTexts = new List<TextDetails>();
        }



        public override void showText(string text, double x, double y, int size, int r, int g, int b)
        {
            int w = 0, h = 0;

            string ffolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts);

            TTF_Font* font = loadFont(ffolder + Path.DirectorySeparatorChar +
                                   "calibri.ttf", size);
            SDL_Color col = new SDL_Color();

            col.r = (byte)r;
            col.g = (byte)g;
            col.b = (byte)b;
            col.a = (byte)255;

            if (font == null)
            {
                Debug.getInstance().log("TTF_OpenFont: " + SDL_GetError());
            }

            TextDetails td = new TextDetails(text, x, y, col, 12);

            td.Font = font;

            SDL_Surface* surf = TTF_RenderText_Solid(td.Font, td.Text, 0, td.Col);
            SDL_Texture* lblText = SDL_CreateTextureFromSurface(_rend, surf);
            SDL_DestroySurface(surf);

            SDL_FRect sRect;

            sRect.x = (float)x;
            sRect.y = (float)y;
            sRect.w = w;
            sRect.h = h;

            SDL_GetTextureSize(lblText, &sRect.w, &sRect.h);

            td.LblText = lblText;

            myTexts.Add(td);


        }
        public override void showText(char[,] text, double x, double y, int size, int r, int g, int b)
        {
            string str = "";
            int row = 0;

            for (int i = 0; i < text.GetLength(0); i++)
            {
                str = "";
                for (int j = 0; j < text.GetLength(1); j++)
                {
                    str += text[j, i];
                }


                showText(str, x, y + (row * size), size, r, g, b);
                row += 1;

            }

        }
    }
}
