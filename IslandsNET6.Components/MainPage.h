#pragma once

#include "MainPage.g.h"

namespace winrt::IslandsNET6::Components::implementation
{
    struct MainPage : MainPageT<MainPage>
    {
        MainPage() 
        {
            // Xaml objects should not call InitializeComponent during construction.
            // See https://github.com/microsoft/cppwinrt/tree/master/nuget#initializecomponent
        }

        void ClickHandler(Windows::Foundation::IInspectable const& sender, Windows::UI::Xaml::RoutedEventArgs const& args);
    };
}

namespace winrt::IslandsNET6::Components::factory_implementation
{
    struct MainPage : MainPageT<MainPage, implementation::MainPage>
    {
    };
}
