#import <SafariServices/SafariServices.h>
extern UIViewController * UnityGetGLViewController();

@class SafariWrapper;

static SFSafariViewController *safariViewController = nil;
static SafariWrapper *wrapperInstance = nil;  

@interface SafariWrapper : NSObject <SFSafariViewControllerDelegate>
- (void)openSafariViewControllerWithURL:(NSString *)urlString;
@end


@implementation SafariWrapper

- (void)openSafariViewControllerWithURL:(NSString *)urlString {
    NSURL *url = [NSURL URLWithString:urlString];
    safariViewController = [[SFSafariViewController alloc] initWithURL:url];
    safariViewController.delegate = self; 
    UIViewController *unityViewController = UnityGetGLViewController();
    [unityViewController presentViewController:safariViewController animated:YES completion:nil];
}

- (void)safariViewControllerDidFinish:(SFSafariViewController *)controller {
    UnitySendMessage("SignInCanvas", "OnSafariClosed", "");
    safariViewController = nil;  
}

@end



extern "C"
{
  void launchUrl(const char * url)
  {
    NSString *urlString = [NSString stringWithUTF8String:url];
    wrapperInstance = [[SafariWrapper alloc] init];
    [wrapperInstance openSafariViewControllerWithURL:urlString];
  }

  void dismiss()
  {
    UIViewController * uvc = UnityGetGLViewController();
    [uvc dismissViewControllerAnimated:YES completion:nil];
  }
}
