/* Copyright 2018 The TensorFlow Authors. All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
==============================================================================*/

using System;
using System.Runtime.InteropServices;
using TfLiteDelegate = System.IntPtr;
using TfLiteInterpreterOptions = System.IntPtr;

namespace TensorFlowLite
{
    /// <summary>
    /// Options for creating a <see cref="Interpreter"/>.
    /// </summary>
    public class InterpreterOptions : IDisposable
    {
        // void (*reporter)(void* user_data, const char* format, va_list args),
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        private delegate void ErrorReporterDelegate(IntPtr userData, string format, IntPtr argsPtrs);

        internal TfLiteInterpreterOptions nativePtr;

        private int _threads;
        public int threads
        {
            get => _threads;
            set
            {
                _threads = value;
                TfLiteInterpreterOptionsSetNumThreads(nativePtr, value);
            }
        }

        private bool _useNNAPI;
        [Obsolete("useNNAPI option is deprecated, use NNAPIDelegate instead.")]
        public bool useNNAPI
        {
            get => _useNNAPI;
            set
            {
                _useNNAPI = value;
            }
        }

        public InterpreterOptions()
        {
            nativePtr = TfLiteInterpreterOptionsCreate();
        }

        public void Dispose()
        {
            if (nativePtr != IntPtr.Zero)
            {
                TfLiteInterpreterOptionsDelete(nativePtr);
                nativePtr = IntPtr.Zero;
            }
        }

        #region Externs
        private const string TensorFlowLibrary = Interpreter.TensorFlowLibrary;

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe TfLiteInterpreterOptions TfLiteInterpreterOptionsCreate();

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe void TfLiteInterpreterOptionsDelete(TfLiteInterpreterOptions options);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe void TfLiteInterpreterOptionsSetNumThreads(
            TfLiteInterpreterOptions options,
            int num_threads
        );

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe void TfLiteInterpreterOptionsAddDelegate(
            TfLiteInterpreterOptions options,
            TfLiteDelegate _delegate);

        #endregion // Externs
    }
}
