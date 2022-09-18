#pragma once

#include "App.g.h"
#include "App.base.h"

namespace winrt::IslandsNET6::Components::implementation
{
    struct App : AppT2<App>
    {
        App() 
        {
            // Xaml objects should not call InitializeComponent during construction.
            // See https://github.com/microsoft/cppwinrt/tree/master/nuget#initializecomponent
        }
    };
}

namespace winrt::IslandsNET6::Components::factory_implementation
{
    struct App : AppT<App, implementation::App>
    {
    };
}
