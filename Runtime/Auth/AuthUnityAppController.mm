#import "UnityAppController.h"
@interface AuthUnityAppController : UnityAppController
void UnitySendMessage( const char * className, const char * methodName, const char * param );
@end
@implementation AuthUnityAppController


- (NSString *)getURLScheme:(NSString *)schemeName {
    NSArray *urlTypes = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleURLTypes"];
    
    for (NSDictionary *urlType in urlTypes) {
        // Hole das Array der URL-Schemes für diesen Typ
        NSString *urlSchemeName = urlType[@"CFBundleURLName"];
              
        if ([urlSchemeName isEqualToString:schemeName]) {
        
            NSArray *urlSchemes = urlType[@"CFBundleURLSchemes"];
            return urlSchemes[0];  
        }
    }
    return nil;
}

- (BOOL) application:(UIApplication*)application
            openURL:(NSURL*)url
            options:(nonnull NSDictionary<UIApplicationOpenURLOptionsKey,id> *)options
{
    NSLog(@"url received %@",url);
    // call the parent implementation
    [super application:application openURL:url options:options];
    if (!url)
        return NO;
    
    NSString *urlScheme = [self getURLScheme:@"youre.id"];
    
    if ([url.scheme isEqualToString:urlScheme]) {
        if ([url.host isEqualToString:@"keycloak_callback"]) {
            if (url.query) {
                const char * queryString = [url.query UTF8String];
                NSLog(@"received auth reply with query string");
                UnitySendMessage("SignInCanvas", "OnAuthReply", queryString);
            } else {
                NSLog(@"received auth reply with no query string");
                UnitySendMessage("SignInCanvas", "OnAuthReply", "");
            }
        } else {
            NSLog(@"received unexpected url host: [%@]", url.host);
        }
    } else {
        NSLog(@"received unexpected url scheme: [%@]", url.scheme);
    }

    return YES;
}
@end
IMPL_APP_CONTROLLER_SUBCLASS(AuthUnityAppController)
