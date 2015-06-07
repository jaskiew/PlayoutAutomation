// FFMpegUtils.h

#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Drawing;

#using <mscorlib.dll>

namespace TAS {
	namespace FFMpegUtils {

	public enum class FieldOrder 
	{
		    UNKNOWN,
			PROGRESSIVE,
			TT,          //< Top coded_first, top displayed first
			BB,          //< Bottom coded first, bottom displayed first
			TB,          //< Top coded first, bottom displayed first
			BT,          //< Bottom coded first, top displayed first
	};

	public ref struct Rational
	{
		int Num;
		int Den;
	};

	// native code
	class _FFMpegWrapper
	{
	private:
		AVFormatContext	*pFormatCtx;
		int64_t countFrames(unsigned int streamIndex);
	public:
		_FFMpegWrapper(char* fileName);
		~_FFMpegWrapper();
		int64_t getFrameCount();
		int64_t getAudioDuration();
		int getHeight();
		int getWidth();
		AVFieldOrder getFieldOrder();
		AVRational getAspectRatio();
		AVRational getFrameRate();
	};

	// managed code
	public ref class FFMpegWrapper
		{
		private: 
			_FFMpegWrapper* wrapper;
		public:
			FFMpegWrapper(String^ fileName);
			~FFMpegWrapper();
			Int64 GetFrameCount();
			int GetHeight();
			int GetWidth();
			TimeSpan^ GetAudioDuration();
			FieldOrder GetFieldOrder();
			Rational^ GetAspectRatio();
			Rational^ GetFrameRate();
			bool GetFrame(TimeSpan fromTime, Bitmap^ destBitmap);
		};
	}
}
