using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;

namespace GreatEscape
{

    public class Screen
    {
        static private int[] m_colors = new int[]{
            (0    << 16) + (0    << 8) + 0,   //black
            (0    << 16) + (0    << 8) + 0xee,
            (0xee << 16) + (0    << 8) + 0,
            (0xee << 16) + (0    << 8) + 0xee,
            (0    << 16) + (0xee << 8) + 0,
            (0    << 16) + (0xee << 8) + 0xee,
            (0xee << 16) + (0xee << 8) + 0,
            (0xee << 16) + (0xee << 8) +  0xee,
        };
        static private int[] m_colors_bright = new int[]{
            (0    << 16) + (0    << 8) + 0,   //black
            (0    << 16) + (0    << 8) + 0xFF,
            (0xFF << 16) + (0    << 8) + 0,
            (0xFF << 16) + (0    << 8) + 0xFF,
            (0    << 16) + (0xFF << 8) + 0,
            (0    << 16) + (0xFF << 8) + 0xFF,
            (0xFF << 16) + (0xFF << 8) + 0,
            (0xFF << 16) + (0xFF << 8) +  0xFF,
        };
        //private int[] m_colors_bright = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };



        /* you fcking idiot, colors are the same, fore or background
        private Brush[] m_brushes = new Brush[]{
        Brushes.Black,
        new SolidBrush(Color.FromArgb(0,    (byte)0,    (byte)0xee)),
        new SolidBrush(Color.FromArgb(0xee, (byte)0,    (byte)0   )),
        new SolidBrush(Color.FromArgb(0xee, (byte)0,    (byte)0xee)),
        new SolidBrush(Color.FromArgb(0,    (byte)0xee, (byte)0   )),
        new SolidBrush(Color.FromArgb(0,    (byte)0xee, (byte)0xee)),
        new SolidBrush(Color.FromArgb(0xee, (byte)0xee, (byte)0   )),
        new SolidBrush(Color.FromArgb(0xee, (byte)0xee, (byte)0xee)),
    };
        private Brush[] m_brushes_bright = new Brush[]{
        Brushes.Black,
        new SolidBrush(Color.FromArgb(0,    (byte)0,    (byte)0xFF)),
        new SolidBrush(Color.FromArgb(0xFF, (byte)0,    (byte)0   )),
        new SolidBrush(Color.FromArgb(0xFF, (byte)0,    (byte)0xFF)),
        new SolidBrush(Color.FromArgb(0,    (byte)0xee, (byte)0   )),
        new SolidBrush(Color.FromArgb(0,    (byte)0xee, (byte)0xFF)),
        new SolidBrush(Color.FromArgb(0xFF, (byte)0xee, (byte)0   )),
        new SolidBrush(Color.FromArgb(0xFF, (byte)0xee, (byte)0xFF)),
    };

        private Color[] m_colors = new Color[]{
        Color.FromArgb(0,    (byte)0,    (byte)0   ),
        Color.FromArgb(0,    (byte)0,    (byte)0xee),
        Color.FromArgb(0xee, (byte)0,    (byte)0   ),
        Color.FromArgb(0xee, (byte)0,    (byte)0xee),
        Color.FromArgb(0,    (byte)0xee, (byte)0   ),
        Color.FromArgb(0,    (byte)0xee, (byte)0xee),
        Color.FromArgb(0xee, (byte)0xee, (byte)0   ),
        Color.FromArgb(0xee, (byte)0xee, (byte)0xee),
    };
        private Color[] m_colors_bright = new Color[]{
        Color.FromArgb(0,    (byte)0,    (byte)0   ),
        Color.FromArgb(0,    (byte)0,    (byte)0xFF),
        Color.FromArgb(0xFF, (byte)0,    (byte)0   ),
        Color.FromArgb(0xFF, (byte)0,    (byte)0xFF),
        Color.FromArgb(0,    (byte)0xee, (byte)0   ),
        Color.FromArgb(0,    (byte)0xee, (byte)0xFF),
        Color.FromArgb(0xFF, (byte)0xee, (byte)0   ),
        Color.FromArgb(0xFF, (byte)0xee, (byte)0xFF),
    };
        */

        public static void PaintZXScreenVersion5(byte[] ram, WriteableBitmap _writeableBitmap)
        {
            //version 5, use wpf writeable bitmap, draw there.
            //Bitmap pic = new Bitmap(256, 192, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //Color cfore, cbackground;
            int cfore, cbackground;

            int screen_start = 16384 + 0;
            int attributes_start = 16384 + 256 * 192 / 8;

            try
            {
                // Reserve the back buffer for updates.
                _writeableBitmap.Lock();

                unsafe
                {
                    // Get a pointer to the back buffer.
                    //IntPtr pBackBuffer = _writeableBitmap.BackBuffer;

                    /*
                    // Find the address of the pixel to draw.
                    pBackBuffer += row * writeableBitmap.BackBufferStride;
                    pBackBuffer += column * 4;

                    // Compute the pixel's color.
                    int color_data = 255 << 16; // R
                    color_data |= 128 << 8;   // G
                    color_data |= 255 << 0;   // B

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) = color_data;
                    */


                    for (int y = 0; y < 192; y++)
                    {

                        //get address y
                        int adr;
                        //adr = screen_start + y * 32;

                        int third = y / 64;
                        int char_row = y % 8;  //this is not row number counted in characters, but row inside a character
                        int block_row = (y % 64) / 8;
                        adr = screen_start + (third * 32 * 8 * 8) + (char_row * 256) + (block_row * 32);

                        //attribute 
                        int adr_attrib = attributes_start + (y / 8) * 32;


                        IntPtr pBackBuffer = _writeableBitmap.BackBuffer;
                        // Find the address of the pixel to draw.
                        pBackBuffer += y * _writeableBitmap.BackBufferStride;
                        //pBackBuffer += column * 4;   we always start from x=0  (column is x)


                        for (int x = 0; x < 256; x += 8)
                        {
                            int atrbyte = ram[adr_attrib];
                            bool bright = (atrbyte & 0x40) > 0;

                            if (bright)
                            {
                                //take 3 last bits
                                cfore = m_colors_bright[atrbyte % 8];
                                //take next 3 bits
                                cbackground = m_colors_bright[atrbyte / 8 % 8];
                            }
                            else
                            {
                                cfore = m_colors[atrbyte % 8];
                                cbackground = m_colors[atrbyte / 8 % 8];
                            }

                            //int colFore = 255; //blue?
                            //int colBack = 0; //black?

                            byte bitmask = 128;
                            for (int i = 0; i < 8; i++)
                            {
                                int membyte = ram[adr];
                                if ((membyte & bitmask) > 0)
                                {
                                    //pic.SetPixel(x + i, y, cfore);
                                    *((int*)pBackBuffer) = cfore;
                                }
                                else
                                {
                                    //pic.SetPixel(x + i, y, cbackground);
                                    *((int*)pBackBuffer) = cbackground;
                                }
                                pBackBuffer += 4;
                                bitmask = (byte)(bitmask >> 1);
                            }

                            adr++;
                            adr_attrib++;

                        }
                    }

                    //?? g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    //?? g.DrawImage(pic, 0, 0, 256 * 3, 192 * 3);

                }

                // Specify the area of the bitmap that changed.
                _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, 256, 192));
            }
            finally
            {
                // Release the back buffer and make it available for display.
                _writeableBitmap.Unlock();
            }
        }//end method Paint










    }



}
